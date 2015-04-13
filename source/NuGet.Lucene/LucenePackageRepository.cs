using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Lucene.Net.Linq;
using NuGet.Lucene.IO;
using Lucene.Net.QueryParsers;
using NuGet.Lucene.Util;
using LuceneDirectory = Lucene.Net.Store.Directory;
#if NET_4_5
using TaskEx=System.Threading.Tasks.Task;
#endif

namespace NuGet.Lucene
{
    public class LucenePackageRepository : LocalPackageRepository, ILucenePackageRepository
    {
        private static readonly ISet<char> AdvancedQueryCharacters = new HashSet<char>(new[] {':', '+', '-', '[', ']', '(', ')'});

        private static readonly ILog Log = LogManager.GetLogger<LucenePackageRepository>();

        public LuceneDataProvider LuceneDataProvider { get; set; }

        public IQueryable<LucenePackage> LucenePackages { get; set; }

        public IPackageIndexer Indexer { get; set; }

        public IHashProvider HashProvider { get; set; }

        public string HashAlgorithmName { get; set; }

        public bool DisablePackageHash { get; set; }

        public string LucenePackageSource { get; set; }

        public PackageOverwriteMode PackageOverwriteMode { get; set; }

        /// <summary>
        /// Flag that enables or disables including list of files on <see cref="LucenePackage.Files"/>.
        /// Setting this flag to <c>true</c> keeps the index smaller when packages with many files
        /// are added. Setting this flag to <c>false</c> enables queries that match packages that
        /// contain a file path.
        ///
        /// Default: <c>false</c>.
        /// </summary>
        public bool IgnorePackageFiles { get; set; }

        private readonly FrameworkCompatibilityTool frameworkCompatibilityTool = new FrameworkCompatibilityTool();

        private readonly object fileSystemLock = new object();

        private volatile int packageCount;
        public int PackageCount { get { return packageCount; } }

        public LucenePackageRepository(IPackagePathResolver packageResolver, IFileSystem fileSystem)
            : base(packageResolver, fileSystem)
        {
        }

        public void Initialize()
        {
            LuceneDataProvider.RegisterCacheWarmingCallback(UpdatePackageCount, () => new LucenePackage(FileSystem));

            UpdatePackageCount(LucenePackages);

            frameworkCompatibilityTool.InitializeKnownFrameworkShortNamesFromIndex(LuceneDataProvider);
        }

        public override string Source
        {
            get { return LucenePackageSource ?? base.Source; }
        }

        public virtual HashingWriteStream CreateStreamForStagingPackage()
        {
            var tmpPath = Path.Combine(StagingDirectory, "package-" + Guid.NewGuid() + ".nupkg.tmp");
            var directoryName = Path.GetDirectoryName(tmpPath);
            if (directoryName != null && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            return new HashingWriteStream(tmpPath, OpenFileWriteStream(tmpPath), HashAlgorithm.Create(HashAlgorithmName));
        }

        public virtual IFastZipPackage LoadStagedPackage(HashingWriteStream packageStream)
        {
            packageStream.Dispose();

            return FastZipPackage.Open(packageStream.FileLocation, packageStream.Hash);
        }

        public void DiscardStagedPackage(HashingWriteStream packageStream)
        {
            packageStream.Dispose();
            FileSystem.DeleteFile(packageStream.FileLocation);
        }

        protected virtual string StagingDirectory
        {
            get { return FileSystem.GetTempFolder(); }
        }

        public async Task AddPackageAsync(IPackage package, CancellationToken cancellationToken)
        {
            var lucenePackage = await DownloadOrMoveOrAddPackageToFileSystemAsync(package, cancellationToken);

            Log.Info(m => m("Indexing package {0} {1}", package.Id, package.Version));

            await Indexer.AddPackageAsync(lucenePackage, cancellationToken);
        }

        private async Task<LucenePackage> DownloadOrMoveOrAddPackageToFileSystemAsync(IPackage package, CancellationToken cancellationToken)
        {
            string temporaryLocation = null;

            try
            {
                if (PackageOverwriteMode == PackageOverwriteMode.Deny && FindPackage(package.Id, package.Version) != null)
                {
                    throw new PackageOverwriteDeniedException(package);
                }

                LucenePackage lucenePackage = null;
                var fastZipPackage = package as IFastZipPackage;
                var dataPackage = package as DataServicePackage;

                if (dataPackage != null)
                {
                    fastZipPackage = await DownloadDataServicePackage(dataPackage, cancellationToken);
                    lucenePackage = Convert(fastZipPackage);
                    lucenePackage.OriginUrl = dataPackage.DownloadUrl;
                    lucenePackage.IsMirrored = true;
                }

                temporaryLocation = fastZipPackage != null ? fastZipPackage.GetFileLocation() : null;

                if (!string.IsNullOrEmpty(temporaryLocation))
                {
                    MoveFileWithOverwrite(temporaryLocation, base.GetPackageFilePath(fastZipPackage));
                    if (lucenePackage == null)
                    {
                        lucenePackage = Convert(fastZipPackage);
                    }

                    return lucenePackage;
                }
                else
                {
                    return await AddPackageToFileSystemAsync(package);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryLocation))
                {
                    FileSystem.DeleteFile(temporaryLocation);
                }
            }
        }

        private Task<LucenePackage> AddPackageToFileSystemAsync(IPackage package)
        {
            Log.Info(m => m("Adding package {0} {1} to file system", package.Id, package.Version));

            lock (fileSystemLock)
            {
                base.AddPackage(package);
            }

            return TaskEx.FromResult(Convert(package));
        }

        private async Task<IFastZipPackage> DownloadDataServicePackage(DataServicePackage dataPackage, CancellationToken cancellationToken)
        {
            var assembly = typeof(LucenePackageRepository).Assembly;

            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(assembly.GetName().Name,
                    assembly.GetName().Version.ToString()));
                client.DefaultRequestHeaders.Add(RepositoryOperationNames.OperationHeaderName,
                    RepositoryOperationNames.Mirror);

                Stream stream;
                using (cancellationToken.Register(client.CancelPendingRequests))
                {
                    stream = await client.GetStreamAsync(dataPackage.DownloadUrl);
                }

                cancellationToken.ThrowIfCancellationRequested();

                using (var hashingStream = CreateStreamForStagingPackage())
                {
                    await stream.CopyToAsync(hashingStream, 4096, cancellationToken);
                    return LoadStagedPackage(hashingStream);
                }
            }
        }

        protected virtual System.Net.Http.HttpClient CreateHttpClient()
        {
            return new System.Net.Http.HttpClient();
        }

        protected virtual Stream OpenFileWriteStream(string path)
        {
            return File.OpenWrite(path);
        }

        protected virtual void MoveFileWithOverwrite(string src, string dest)
        {
            dest = FileSystem.GetFullPath(dest);

            lock (fileSystemLock)
            {
                var parent = Path.GetDirectoryName(dest);
                if (parent != null && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }

                Log.Info(m => m("Moving package file from {0} to {1}.", src, dest));
                File.Move(src, dest);
            }
        }

        public override void AddPackage(IPackage package)
        {
            var task = TaskEx.Run(async () => await AddPackageAsync(package, CancellationToken.None));
            task.Wait();
        }

        public async Task IncrementDownloadCountAsync(IPackage package, CancellationToken cancellationToken)
        {
            await Indexer.IncrementDownloadCountAsync(package, cancellationToken);
        }

        private void UpdatePackageCount(IQueryable<LucenePackage> packages)
        {
            packageCount = packages.Count();

            Log.Info(m => m("Refreshing index. Package count: " + packageCount));
        }

        public async Task RemovePackageAsync(IPackage package, CancellationToken cancellationToken)
        {
            var task = Indexer.RemovePackageAsync(package, cancellationToken);

            lock (fileSystemLock)
            {
                base.RemovePackage(package);
            }

            await task;
        }

        public override void RemovePackage(IPackage package)
        {
            var task = TaskEx.Run(async () => await RemovePackageAsync(package, CancellationToken.None));
            task.Wait();
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return LucenePackages;
        }

        public override IPackage FindPackage(string packageId, SemanticVersion version)
        {
            return FindLucenePackage(packageId, version);
        }

        public LucenePackage FindLucenePackage(string packageId, SemanticVersion version)
        {
            var packages = LucenePackages;

            var matches = from p in packages
                          where p.Id == packageId && p.Version == new StrictSemanticVersion(version)
                          select p;

            return matches.SingleOrDefault();
        }

        public override IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            return LucenePackages.Where(p => p.Id == packageId);
        }

        public IEnumerable<string> GetAvailableSearchFieldNames()
        {
            var propertyNames =  LuceneDataProvider.GetIndexedPropertyNames<LucenePackage>();
            var aliasMap = NuGetQueryParser.IndexedPropertyAliases;
            return propertyNames.Except(aliasMap.Values).Union(aliasMap.Keys);
        }

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            return Search(new SearchCriteria(searchTerm)
                {
                    TargetFrameworks = targetFrameworks,
                    AllowPrereleaseVersions = allowPrereleaseVersions
                });
        }

        public IQueryable<IPackage> Search(SearchCriteria criteria)
        {
            var packages = LucenePackages;

            if (!string.IsNullOrEmpty(criteria.SearchTerm))
            {
                packages = ApplySearchCriteria(criteria, packages);
            }

            if (!criteria.AllowPrereleaseVersions)
            {
                packages = packages.Where(p => !p.IsPrerelease);
            }

            if (criteria.TargetFrameworks != null && criteria.TargetFrameworks.Any())
            {
                packages = ApplyTargetFrameworkFilter(criteria, packages);
            }

            if (criteria.PackageOriginFilter != PackageOriginFilter.Any)
            {
                var flag = criteria.PackageOriginFilter == PackageOriginFilter.Mirror;
                packages = packages.Where(p => p.IsMirrored == flag);
            }

            packages = ApplySort(criteria, packages);

            return packages;
        }

        protected virtual IQueryable<LucenePackage> ApplyTargetFrameworkFilter(SearchCriteria criteria, IQueryable<LucenePackage> packages)
        {
            return frameworkCompatibilityTool.FilterByTargetFramework(packages, criteria.TargetFrameworks.First());
        }

        protected virtual IQueryable<LucenePackage> ApplySearchCriteria(SearchCriteria criteria, IQueryable<LucenePackage> packages)
        {
            var advancedQuerySyntax = criteria.SearchTerm.Any(c => AdvancedQueryCharacters.Contains(c));

            if (advancedQuerySyntax)
            {
                var queryParser = new NuGetQueryParser(LuceneDataProvider.CreateQueryParser<LucenePackage>())
                {
                    AllowLeadingWildcard = true
                };

                try
                {
                    var query = queryParser.Parse(criteria.SearchTerm);
                    return packages.Where(query);
                }
                catch (ParseException ex)
                {
                    throw new InvalidSearchCriteriaException("Failed to parse query", ex);
                }
            }

            return from
                pkg in packages
                where
                    ((pkg.Id == criteria.SearchTerm).Boost(5) ||
                     (pkg.SearchId == criteria.SearchTerm).Boost(4) ||
                     (pkg.Title == criteria.SearchTerm).Boost(3) ||
                     (pkg.Tags == criteria.SearchTerm).Boost(2) ||
                     (pkg.Authors.Contains(criteria.SearchTerm) || pkg.Owners.Contains(criteria.SearchTerm)).Boost(2) ||
                     (pkg.Files.Contains(criteria.SearchTerm)) ||
                     (pkg.Summary == criteria.SearchTerm || pkg.Description == criteria.SearchTerm)).AllowSpecialCharacters()
                select
                    pkg;
        }

        private static IQueryable<LucenePackage> ApplySort(SearchCriteria criteria, IQueryable<LucenePackage> packages)
        {
            Expression<Func<LucenePackage, object>> sortSelector = null;

            switch (criteria.SortField)
            {
                case SearchSortField.Id:
                    sortSelector = p => p.Id;
                    break;
                case SearchSortField.Title:
                    sortSelector = p => p.Title;
                    break;
                case SearchSortField.Published:
                    sortSelector = p => p.Published;
                    break;
                case SearchSortField.Score:
                    sortSelector = p => p.Score();
                    break;
            }

            if (sortSelector == null)
            {
                return packages;
            }

            var orderedPackages = criteria.SortDirection == SearchSortDirection.Ascending
                    ? packages.OrderBy(sortSelector)
                    : packages.OrderByDescending(sortSelector);

            if (criteria.SortField == SearchSortField.Id)
            {
                orderedPackages = orderedPackages.ThenByDescending(p => p.Version);
            }

            return orderedPackages;
        }

        public IEnumerable<IPackage> GetUpdates(IEnumerable<IPackageName> packages, bool includePrerelease, bool includeAllVersions,
            IEnumerable<FrameworkName> targetFrameworks, IEnumerable<IVersionSpec> versionConstraints)
        {
            var baseQuery = LucenePackages;

            if (!includePrerelease)
            {
                baseQuery = baseQuery.Where(pkg => !pkg.IsPrerelease);
            }

            var targetFrameworkList = (targetFrameworks ?? Enumerable.Empty<FrameworkName>()).ToList();
            var versionConstraintList = (versionConstraints ?? Enumerable.Empty<IVersionSpec>()).ToList();

            var results = new List<IPackage>();
            var i = 0;

            foreach (var current in packages)
            {
                var ii = i;
                var id = current.Id;
                var currentVersion = new StrictSemanticVersion(current.Version);
                var matchedPackages = (IEnumerable<LucenePackage>)baseQuery.Where(pkg => pkg.Id == id).OrderBy(pkg => pkg.Version).ToList();

                if (targetFrameworkList.Any())
                {
                    matchedPackages = matchedPackages.Where(pkg => targetFrameworkList.Any(fwk => VersionUtility.IsCompatible(fwk, pkg.GetSupportedFrameworks())));
                }

                matchedPackages = matchedPackages.Where(pkg => pkg.Version > currentVersion);

                if (versionConstraintList.Any() && versionConstraintList[ii] != null)
                {
                    matchedPackages = matchedPackages.Where(pkg => versionConstraintList[ii].Satisfies(pkg.Version.SemanticVersion));
                }

                if (includeAllVersions)
                {
                    results.AddRange(matchedPackages);
                }
                else
                {
                    var latest = matchedPackages.LastOrDefault();
                    if (latest != null)
                    {
                        results.Add(latest);
                    }
                }

                i++;
            }

            return results;
        }

        public Task SynchronizeWithFileSystem(SynchronizationMode mode, CancellationToken cancellationToken)
        {
            Log.Info(m => m("Synchronizing packages with filesystem."));
            return Indexer.SynchronizeIndexWithFileSystemAsync(mode, cancellationToken);
        }

        public RepositoryInfo GetStatus()
        {
            return new RepositoryInfo(packageCount, Indexer.GetIndexingStatus());
        }

        public void Optimize()
        {
            Indexer.Optimize();
        }

        public IObservable<RepositoryInfo> StatusChanged
        {
            get
            {
                return Indexer.StatusChanged.Select(s => new RepositoryInfo(packageCount, s));
            }
        }

        public LucenePackage LoadFromIndex(string path)
        {
            var relativePath = FileSystem.MakeRelative(path);
            var results = from p in LucenePackages where p.Path == relativePath select p;
            return results.SingleOrDefault();
        }

        public LucenePackage LoadFromFileSystem(string path)
        {
            var fullPath = FileSystem.GetFullPath(path);
            var package = Convert(OpenPackage(fullPath), new LucenePackage(FileSystem) { Path = FileSystem.MakeRelative(fullPath) });
            return package;
        }

        protected override string GetPackageFilePath(IPackage package)
        {
            return GetPackageFilePath(package);
        }

        protected override string GetPackageFilePath(string id, SemanticVersion version)
        {
            return GetPackageFilePath(new PackageName(id, version));
        }

        protected string GetPackageFilePath(IPackageName package)
        {
            var lucenePackage = package as LucenePackage ?? FindLucenePackage(package.Id, package.Version);
            if (lucenePackage != null && !string.IsNullOrEmpty(lucenePackage.Path))
            {
                return lucenePackage.Path;
            }

            return base.GetPackageFilePath(package.Id, package.Version);
        }

        protected override IPackage OpenPackage(string path)
        {
            if (DisablePackageHash)
            {
                return FastZipPackage.Open(path, new byte[0]);
            }

            return FastZipPackage.Open(path, HashProvider);
        }

        public LucenePackage Convert(IPackage package)
        {
            var lucenePackage = package as LucenePackage;
            if (lucenePackage == null)
            {
                lucenePackage = new LucenePackage(FileSystem);

                Convert(package, lucenePackage);
            }

            frameworkCompatibilityTool.AddKnownFrameworkShortNames(lucenePackage.SupportedFrameworks);

            return lucenePackage;
        }

        private LucenePackage Convert(IPackage package, LucenePackage lucenePackage)
        {
            CopyPackageData(package, lucenePackage);

            if (string.IsNullOrWhiteSpace(lucenePackage.Path))
            {
                lucenePackage.Path = GetPackageFilePath(lucenePackage);
            }

            CalculateDerivedData(package, lucenePackage, lucenePackage.Path, lucenePackage.GetStream);

            return lucenePackage;
        }

        private void CopyPackageData(IPackage package, LucenePackage lucenePackage)
        {
            lucenePackage.Id = package.Id;
            lucenePackage.Version = new StrictSemanticVersion(package.Version.ToString());
            lucenePackage.MinClientVersion = package.MinClientVersion;
            lucenePackage.Title = package.Title;
            lucenePackage.Authors = (package.Authors ?? Enumerable.Empty<string>()).Select(i => i.Trim()).ToArray();
            lucenePackage.Owners = (package.Owners ?? Enumerable.Empty<string>()).Select(i => i.Trim()).ToArray();
            lucenePackage.IconUrl = FilterPlaceholderUri(package.IconUrl);
            lucenePackage.LicenseUrl = FilterPlaceholderUri(package.LicenseUrl);
            lucenePackage.ProjectUrl = FilterPlaceholderUri(package.ProjectUrl);
            lucenePackage.RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            lucenePackage.Description = package.Description;
            lucenePackage.Summary = package.Summary;
            lucenePackage.ReleaseNotes = package.ReleaseNotes;
            lucenePackage.Language = package.Language;
            lucenePackage.Tags = package.Tags;
            lucenePackage.Copyright = package.Copyright;
            lucenePackage.FrameworkAssemblies = package.FrameworkAssemblies;
            lucenePackage.DependencySets = package.DependencySets;
            lucenePackage.ReportAbuseUrl = package.ReportAbuseUrl;
            lucenePackage.DownloadCount = package.DownloadCount;
            lucenePackage.IsAbsoluteLatestVersion = package.IsAbsoluteLatestVersion;
            lucenePackage.IsLatestVersion = package.IsLatestVersion;
            lucenePackage.Listed = package.Listed;
            lucenePackage.Published = package.Published;
            lucenePackage.AssemblyReferences = package.AssemblyReferences;
            lucenePackage.PackageAssemblyReferences = package.PackageAssemblyReferences;
            lucenePackage.DevelopmentDependency = package.DevelopmentDependency;
        }

        private Uri FilterPlaceholderUri(Uri uri)
        {
            if (uri != null && uri.IsAbsoluteUri && uri.Host.ToLowerInvariant().Contains("url_here_or_delete_this_line"))
            {
                return null;
            }

            return uri;
        }

        protected virtual void CalculateDerivedData(IPackage sourcePackage, LucenePackage package, string path, Func<Stream> openStream)
        {
            var fastPackage = sourcePackage as FastZipPackage;
            if (fastPackage == null)
            {
                CalculateDerivedDataFromStream(package, openStream);
            }
            else
            {
                package.PackageSize = fastPackage.Size;
                package.PackageHash = System.Convert.ToBase64String(fastPackage.Hash);
                package.Created = fastPackage.Created;
            }

            package.PackageHashAlgorithm = HashAlgorithmName;
            package.LastUpdated = GetLastModified(package, path);
            package.Published = package.LastUpdated;
            package.Path = path;
            package.SupportedFrameworks = sourcePackage.GetSupportedFrameworks().Select(VersionUtility.GetShortFrameworkName);

            var files = IgnorePackageFiles
                ? Enumerable.Empty<string>()
                : sourcePackage.GetFiles().Select(f => f.Path).ToArray();

            package.Files = files;
        }

        private DateTimeOffset GetLastModified(LucenePackage package, string path)
        {
            var lastModified = FileSystem.GetLastModified(path);
            if (lastModified.Year <= 1700)
            {
                lastModified = package.Created;
            }
            return lastModified;
        }

        private void CalculateDerivedDataFromStream(LucenePackage package, Func<Stream> openStream)
        {
            using (var stream = openStream())
            {
                if (!stream.CanSeek)
                {
                    throw new InvalidOperationException("Package stream must support CanSeek.");
                }

                package.Created = FastZipPackageBase.GetPackageCreatedDateTime(stream);
                package.PackageSize = stream.Length;

                stream.Seek(0, SeekOrigin.Begin);
                package.PackageHash = System.Convert.ToBase64String(HashProvider.CalculateHash(stream));
            }
        }
    }
}
