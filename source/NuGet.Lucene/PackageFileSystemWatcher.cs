using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;

#if NET_4_5
using TaskEx=System.Threading.Tasks.Task;
#endif

namespace NuGet.Lucene
{
    public class PackageFileSystemWatcher : IDisposable
    {
        private FileSystemWatcher fileWatcher;
        private IDisposable fileObserver;
        private IDisposable dirObserver;

        public ILog Log { get; set; }

        public IFileSystem FileSystem { get; set; }

        public ILucenePackageRepository PackageRepository { get; set; }

        public IPackageIndexer Indexer { get; set; }

        /// <summary>
        /// Sets the amount of time to wait after receiving a <c cref="FileSystemWatcher.Changed">Changed</c>
        /// event before attempting to index a package. This timeout is meant to avoid trying to read a package
        /// while it is still being built or copied into place.
        /// </summary>
        public TimeSpan QuietTime { get; set; }

        public PackageFileSystemWatcher()
        {
            Log = LogManager.GetLogger<PackageFileSystemWatcher>();
            QuietTime = TimeSpan.FromSeconds(3);
        }

        public void Initialize()
        {
            fileWatcher = new FileSystemWatcher(FileSystem.Root, "*.nupkg")
                {
                    NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true
                };

            var modifiedFilesThrottledByPath = ModifiedFiles
                .Select(args => args.EventArgs.FullPath)
                .GroupBy(path => path)
                .Select(groupByPath => groupByPath.Throttle(QuietTime))
                .SelectMany(obs => obs);
            
            fileObserver = modifiedFilesThrottledByPath.Subscribe(async path => await OnPackageModified(path));
            fileWatcher.Deleted += async (s, e) => await OnPackageDeleted(e.FullPath);
            fileWatcher.Renamed += async (s, e) => await OnPackageRenamed(e.OldFullPath, e.FullPath);
            fileWatcher.Error += OnFileWatcherError;

            fileWatcher.EnableRaisingEvents = true;

            dirObserver = MovedDirectories.Select(args => args.EventArgs.FullPath).Throttle(QuietTime).Subscribe(OnDirectoryMoved);
        }

        public void Dispose()
        {
            fileObserver.Dispose();
            fileWatcher.Dispose();
            dirObserver.Dispose();
        }

        public void OnDirectoryMoved(string fullPath)
        {
            try
            {
                if (FileSystem.GetFiles(fullPath, "*.nupkg", true).IsEmpty()) return;
            }
            catch (IOException ex)
            {
                Log.Error(ex);
                return;
            }

            Indexer.SynchronizeIndexWithFileSystemAsync(CancellationToken.None);
        }

        public async Task OnPackageModified(string fullPath)
        {
            Log.Info(m => m("Indexing modified package " + fullPath));
            await AddToIndex(fullPath).ContinueWith(LogOnFault);
        }

        public async Task OnPackageRenamed(string oldFullPath, string fullPath)
        {
            Log.Info(m => m("Package path {0} renamed to {1}.", oldFullPath, fullPath));

            var task = RemoveFromIndex(oldFullPath).ContinueWith(LogOnFault);
            
            if (fullPath.EndsWith(Constants.PackageExtension))
            {
                var addToIndex = AddToIndex(fullPath).ContinueWith(LogOnFault);
                await TaskEx.WhenAll(addToIndex, task);
                return;
            }
            
            await task;
        }

        public async Task OnPackageDeleted(string fullPath)
        {
            Log.Info(m => m("Package path {0} deleted.", fullPath));

            await RemoveFromIndex(fullPath).ContinueWith(LogOnFault);
        }

        private async Task AddToIndex(string fullPath)
        {
            LucenePackage package = null;

            Action checkTimestampAndLoadPackage = () =>
                {
                    var existingPackage = PackageRepository.LoadFromIndex(fullPath);

                    var flag = (existingPackage == null ||
                            IndexDifferenceCalculator.TimestampsMismatch(existingPackage,
                                                                         FileSystem.GetLastModified(fullPath)));
                    if (!flag) return;

                    package = PackageRepository.LoadFromFileSystem(fullPath);
                };

            await Task.Factory.StartNew(checkTimestampAndLoadPackage);

            if (package != null)
            {
                await Indexer.AddPackageAsync(package, CancellationToken.None);
            }
        }

        private async Task RemoveFromIndex(string fullPath)
        {
            var package = PackageRepository.LoadFromIndex(fullPath);
            if (package != null)
            {
                await Indexer.RemovePackageAsync(package, CancellationToken.None);
            }
        }

        private void LogOnFault(Task task)
        {
            if (task.IsFaulted)
            {
                task.Exception.Handle(ex => { Log.Error(ex); return true; });
            }
        }

        private void OnFileWatcherError(object source, ErrorEventArgs e)
        {
            if (e.GetException() is InternalBufferOverflowException)
            {
                Log.Warn(m => m("InternalBufferOverflowException in FileSystemWatcher; forcing full synchronization."));
                Indexer.SynchronizeIndexWithFileSystemAsync(CancellationToken.None);
            }
            else
            {
                Log.Error(m => m("Unhandled error in FileSystemWatcher"), e.GetException());
            }
        }

        private IObservable<EventPattern<FileSystemEventArgs>> ModifiedFiles
        {
            get
            {
                var created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    eh => eh.Invoke,
                    eh => fileWatcher.Created += eh,
                    eh => fileWatcher.Created -= eh);
                var changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    eh => eh.Invoke,
                    eh => fileWatcher.Changed += eh,
                    eh => fileWatcher.Changed -= eh);

                return created.Merge(changed);
            }
        }

        private IObservable<EventPattern<FileSystemEventArgs>> MovedDirectories
        {
            get
            {
                Func<FileSystemWatcher> createDirWatcher = () =>
                    {
                        var dirWatcher = new FileSystemWatcher(FileSystem.Root)
                                                        {
                                                            NotifyFilter = NotifyFilters.DirectoryName,
                                                            IncludeSubdirectories = true,
                                                            EnableRaisingEvents = true,
                                                        };

                        dirWatcher.Error += OnFileWatcherError;
                        return dirWatcher;
                    };

                Func<FileSystemWatcher, IObservable<EventPattern<FileSystemEventArgs>>> createObservable = dirWatcher =>
                {
                    var created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        eh => eh.Invoke,
                        eh => dirWatcher.Created += eh,
                        eh => dirWatcher.Created -= eh);
                    var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                        eh => eh.Invoke,
                        eh => dirWatcher.Renamed += eh,
                        eh => dirWatcher.Renamed -= eh);

                    return created.Merge(renamed.Select(re => new EventPattern<FileSystemEventArgs>(re.Sender, re.EventArgs)));
                };

                return Observable.Using(createDirWatcher, createObservable);
            }
        }

    }
}
