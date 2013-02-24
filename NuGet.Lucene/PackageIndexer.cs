using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Lucene.Net.Index;
using Lucene.Net.Linq;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Search;
using NuGet.Lucene.Util;

namespace NuGet.Lucene
{
    public class PackageIndexer : IPackageIndexer, IDisposable
    {
        public static ILog Log = LogManager.GetLogger<PackageIndexer>();

        private enum UpdateType { Add, Remove, RemoveByPath, Increment }
        private class Update
        {
            private readonly LucenePackage package;
            private readonly UpdateType updateType;
            private readonly TaskCompletionSource<object> signal = new TaskCompletionSource<object>();

            public Update(LucenePackage package, UpdateType updateType)
            {
                this.package = package;
                this.updateType = updateType;
            }

            public LucenePackage Package
            {
                get { return package; }
            }

            public UpdateType UpdateType
            {
                get { return updateType; }
            }

            public Task Task
            {
                get
                {
                    return signal.Task;
                }
            }

            public void SetComplete()
            {
                if (!signal.Task.IsCompleted)
                {
                    signal.SetResult(null);
                }
            }

            public void SetException(Exception exception)
            {
                signal.SetException(exception);
            }
        }

        private volatile IndexingState indexingState = IndexingState.Idle;
        private volatile SynchronizationStatus synchronizationStatus = new SynchronizationStatus(SynchronizationState.Idle);
        private readonly BlockingCollection<Update> pendingUpdates = new BlockingCollection<Update>();

        private Task indexUpdaterTask;

        public IFileSystem FileSystem { get; set; }

        public IIndexWriter Writer { get; set; }

        public LuceneDataProvider Provider { get; set; }

        public ILucenePackageRepository PackageRepository { get; set; }

        private event EventHandler statusChanged;

        public void Initialize()
        {
            indexUpdaterTask = Task.Factory.StartNew(IndexUpdateLoop, TaskCreationOptions.LongRunning);
        }

        public void Dispose()
        {
            pendingUpdates.CompleteAdding();
            indexUpdaterTask.Wait();
        }

        /// <summary>
        /// Gets status of index building activity.
        /// </summary>
        public IndexingStatus GetIndexingStatus()
        {
            using (var reader = Writer.GetReader())
            {
                return new IndexingStatus(
                    indexingState,
                    synchronizationStatus,
                    reader.NumDocs(),
                    reader.NumDeletedDocs,
                    reader.IsOptimized(),
                    DateTimeUtils.FromJava(reader.IndexCommit.Timestamp));
            }
        }

        public IObservable<IndexingStatus> StatusChanged
        {
            get
            {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                        eh => eh.Invoke,
                        eh => statusChanged += eh,
                        eh => statusChanged -= eh)
                .Select(_ => GetIndexingStatus());
            }
        }

        public void Optimize()
        {
            using (UpdateStatus(IndexingState.Optimizing))
            {
                Writer.Optimize();
            }
        }

        public Task SynchronizeIndexWithFileSystem(CancellationToken cancellationToken)
        {
            IndexDifferences differences = null;
            Action findDifferences = () =>
                {
                    using (UpdateSynchronizationStatus(SynchronizationState.Scanning))
                    {
                        using (var session = OpenSession())
                        {
                            differences = IndexDifferenceCalculator.FindDifferences(FileSystem, session.Query(), cancellationToken);
                        }
                    }
                };

            return Task.Run(findDifferences, cancellationToken)
                .ContinueWith(task => SynchronizeIndexWithFileSystem(differences, cancellationToken), cancellationToken, TaskContinuationOptions.NotOnFaulted, TaskScheduler.Default);
        }

        public Task AddPackage(LucenePackage package)
        {
            var update = new Update(package, UpdateType.Add);
            pendingUpdates.Add(update);
            return update.Task;
        }

        public Task RemovePackage(IPackage package)
        {
            if (!(package is LucenePackage)) throw new ArgumentException("Package of type " + package.GetType() + " not supported.");

            var update = new Update((LucenePackage)package, UpdateType.Remove);
            pendingUpdates.Add(update);
            return update.Task;
        }

        public Task IncrementDownloadCount(IPackage package)
        {
            if (!(package is LucenePackage)) throw new ArgumentException("Package of type " + package.GetType() + " not supported.");

            if (string.IsNullOrWhiteSpace(package.Id))
            {
                throw new InvalidOperationException("Package Id must be specified.");
            }

            if (package.Version == null)
            {
                throw new InvalidOperationException("Package Version must be specified.");
            }

            var update = new Update((LucenePackage) package, UpdateType.Increment);
            pendingUpdates.Add(update);
            return update.Task;
        }

        internal void SynchronizeIndexWithFileSystem(IndexDifferences diff, CancellationToken cancellationToken)
        {
            if (diff.IsEmpty) return;

            var tasks = new ConcurrentQueue<Task>();

            Log.Info(string.Format("Updates to process: {0} packages added, {1} packages updated, {2} packages removed.", diff.NewPackages.Count(), diff.ModifiedPackages.Count(), diff.MissingPackages.Count()));

            foreach (var path in diff.MissingPackages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var package = new LucenePackage(FileSystem) { Path = path };
                var update = new Update(package, UpdateType.RemoveByPath);
                pendingUpdates.Add(update);
                tasks.Enqueue(update.Task);
            }
            
            var pathsToIndex = diff.NewPackages.Union(diff.ModifiedPackages).OrderBy(p => p).ToArray();

            var i = 0;

            Parallel.ForEach(pathsToIndex, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (p, s) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using(UpdateSynchronizationStatus(SynchronizationState.Building, completedPackages: Interlocked.Increment(ref i), packagesToIndex: pathsToIndex.Length, currentPackagePath: p))
                    {
                        tasks.Enqueue(SynchronizePackage(p));
                    }
                });

            Task.WaitAll(tasks.ToArray(), cancellationToken);
        }

        private Task SynchronizePackage(string path)
        {
            try
            {
                var package = PackageRepository.LoadFromFileSystem(path);
                return AddPackage(package);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to index package path: " + path, ex);
                return Task.FromResult(ex);
            }
        }
        
        private void IndexUpdateLoop()
        {
            var items = new List<Update>();

            while (!pendingUpdates.IsCompleted)
            {
                items.Clear();

                try
                {
                    pendingUpdates.TakeAvailable(items, Timeout.InfiniteTimeSpan);
                }
                catch (OperationCanceledException)
                {
                }

                if (items.Any())
                {
                    ApplyUpdates(items);
                }
            }

            Log.Info("Update task shutting down.");
        }

        private void ApplyUpdates(List<Update> items)
        {
            Log.Info(m => m("Processing {0} updates.", items.Count()));

            using (var session = OpenSession())
            {
                using (UpdateStatus(IndexingState.Updating))
                {
                    var removals =
                        items.Where(i => i.UpdateType == UpdateType.Remove).ToList();
                    removals.ForEach(pkg => RemovePackageInternal(pkg, session));

                    var removalsByPath =
                        items.Where(i => i.UpdateType == UpdateType.RemoveByPath).ToList();
                    RemovePackagesByPath(removalsByPath, session);

                    var additions = items.Where(i => i.UpdateType == UpdateType.Add).ToList();
                    ApplyPendingAdditions(additions, session);

                    var downloadUpdates =
                        items.Where(i => i.UpdateType == UpdateType.Increment).Select(i => i.Package).ToList();
                    ApplyPendingDownloadIncrements(downloadUpdates, session);
                }

                using (UpdateStatus(IndexingState.Committing))
                {
                    session.Commit();
                    items.ForEach(i => i.SetComplete());
                }
            }
        }

        private static void RemovePackagesByPath(IEnumerable<Update> removalsByPath, ISession<LucenePackage> session)
        {
            var deleteQueries = removalsByPath.Select(p => (Query) new TermQuery(new Term("Path", p.Package.Path))).ToArray();
            session.Delete(deleteQueries);
        }

        private void ApplyPendingAdditions(List<Update> additions, ISession<LucenePackage> session)
        {
            foreach (var grouping in additions.GroupBy(update => update.Package.Id))
            {
                try
                {
                    AddPackagesInternal(grouping.Key, grouping.Select(p => p.Package).ToList(), session);
                }
                catch (Exception ex)
                {
                    additions.ForEach(i => i.SetException(ex));
                }
            }
        }

        private void AddPackagesInternal(string packageId, IEnumerable<LucenePackage> packages, ISession<LucenePackage> session)
        {
            var currentPackages = (from p in session.Query()
                                   where p.Id == packageId
                                   orderby p.Version descending
                                   select p).ToList();

            var newest = currentPackages.FirstOrDefault();
            var versionDownloadCount = newest != null ? newest.VersionDownloadCount : 0;

            foreach (var package in packages)
            {
                var packageToReplace = currentPackages.Find(p => p.Version == package.Version);

                package.VersionDownloadCount = versionDownloadCount;
                package.DownloadCount = packageToReplace != null ? packageToReplace.DownloadCount : 0;

                currentPackages.Remove(packageToReplace);
                currentPackages.Add(package);

                session.Add(package);
            }

            UpdatePackageVersionFlags(currentPackages.OrderByDescending(p => p.Version));
        }

        private void RemovePackageInternal(Update update, ISession<LucenePackage> session)
        {
            try
            {
                session.Delete(update.Package);

                var remainingPackages = from p in session.Query()
                                        where p.Id == update.Package.Id
                                        orderby p.Version descending
                                        select p;

                UpdatePackageVersionFlags(remainingPackages);
            }
            catch (Exception e)
            {
                update.SetException(e);
            }
        }

        private void UpdatePackageVersionFlags(IEnumerable<LucenePackage> packages)
        {
            var first = true;
            foreach (var p in packages)
            {
                p.IsLatestVersion = first;
                p.IsAbsoluteLatestVersion = first;

                if (first)
                {
                    first = false;
                }
            }
        }

        public void ApplyPendingDownloadIncrements(IList<LucenePackage> increments, ISession<LucenePackage> session)
        {
            if (increments.Count == 0) return;

            var byId = increments.ToLookup(p => p.Id);

            foreach (var grouping in byId)
            {
                var packageId = grouping.Key;
                var packages = from p in session.Query() where p.Id == packageId select p;
                var byVersion = grouping.ToLookup(p => p.Version);

                foreach (var lucenePackage in packages)
                {
                    lucenePackage.DownloadCount += grouping.Count();
                    lucenePackage.VersionDownloadCount += byVersion[lucenePackage.Version].Count();
                }
            }
        }

        protected internal virtual ISession<LucenePackage> OpenSession()
        {
            return Provider.OpenSession(() => new LucenePackage(FileSystem));
        }

        private IDisposable UpdateSynchronizationStatus(SynchronizationState state, int completedPackages = 0, int packagesToIndex = 0, string currentPackagePath = null)
        {
            synchronizationStatus = new SynchronizationStatus(
                    state,
                    currentPackagePath,
                    completedPackages,
                    packagesToIndex
                );

            RaiseStatusChanged();

            return new DisposableAction(() =>
                {
                    synchronizationStatus = new SynchronizationStatus(SynchronizationState.Idle);
                    RaiseStatusChanged();
                });
        }

        private IDisposable UpdateStatus(IndexingState state)
        {
            indexingState = state;
            RaiseStatusChanged();

            return new DisposableAction(() =>
            {
                indexingState = IndexingState.Idle;
                RaiseStatusChanged();
            });
        }

        private void RaiseStatusChanged()
        {
            var tmp = statusChanged;

            if (tmp != null)
            {
                tmp(this, new EventArgs());
            }
        }
    }
}