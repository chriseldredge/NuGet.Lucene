using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using ICSharpCode.SharpZipLib.Zip;
using Lucene.Net.Linq;
using NuGet.Lucene.Util;
using LuceneDirectory = Lucene.Net.Store.Directory;

#if NET_4_5
using TaskEx=System.Threading.Tasks.Task;
#endif

namespace NuGet.Lucene
{
    public class LucenePackageRepository : LocalPackageRepository, ILucenePackageRepository
    {
        private static readonly ILog Log = LogManager.GetLogger<LucenePackageRepository>();

        public LuceneDataProvider LuceneDataProvider { get; set; }

        public IQueryable<LucenePackage> LucenePackages { get; set; }

        public IPackageIndexer Indexer { get; set; }

        public IHashProvider HashProvider { get; set; }

        public string HashAlgorithm { get; set; }

        public string LucenePackageSource { get; set; }

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
        }

        public override string Source
        {
            get { return LucenePackageSource ?? base.Source; }
        }

        public async Task AddPackageAsync(IPackage package, CancellationToken cancellationToken)
        {
            Log.Info(m => m("Adding package {0} {1} to file system", package.Id, package.Version));

            var lucenePackage = await AddPackageToFileSystemAsync(package, cancellationToken);

            Log.Info(m => m("Indexing package {0} {1}", package.Id, package.Version));

            await Indexer.AddPackageAsync(lucenePackage, cancellationToken);
        }

        private async Task<LucenePackage> AddPackageToFileSystemAsync(IPackage package, CancellationToken cancellationToken)
        {
            var dataPackage = package as DataServicePackage;

            if (dataPackage == null)
            {
                base.AddPackage(package);
                return Convert(package);
            }

            return await DownloadDataServicePackage(dataPackage, cancellationToken);
        }

        private async Task<LucenePackage> DownloadDataServicePackage(DataServicePackage dataPackage, CancellationToken cancellationToken)
        {
            var parent = PathResolver.GetInstallPath(dataPackage);
            var path = Path.Combine(parent, PathResolver.GetPackageFileName(dataPackage));

            if (!Directory.Exists(parent))
            {
                Directory.CreateDirectory(parent);
            }

            var client = CreateHttpClient();
            var assembly = typeof (LucenePackageRepository).Assembly;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(assembly.GetName().Name,
                assembly.GetName().Version.ToString()));
            client.DefaultRequestHeaders.Add(RepositoryOperationNames.OperationHeaderName, RepositoryOperationNames.Mirror);
            Stream stream;
            using (cancellationToken.Register(client.CancelPendingRequests))
            {
                stream = await client.GetStreamAsync(dataPackage.DownloadUrl);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var fileStream = OpenFileWriteStream(path);
            using (fileStream)
            {
                using (stream)
                {
                    await stream.CopyToAsync(fileStream, 4096, cancellationToken);
                }
            }

            var lucenePackage = LoadFromFileSystem(path);
            lucenePackage.OriginUrl = dataPackage.DownloadUrl;
            lucenePackage.IsMirrored = true;
            return lucenePackage;
        }

        protected virtual System.Net.Http.HttpClient CreateHttpClient()
        {
            return new System.Net.Http.HttpClient();
        }

        protected virtual Stream OpenFileWriteStream(string path)
        {
            return File.OpenWrite(path);
        }

        public override void AddPackage(IPackage package)
        {
            var task = AddPackageAsync(package, CancellationToken.None);
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

            base.RemovePackage(package);

            await task;
        }
        
        public override void RemovePackage(IPackage package)
        {
            RemovePackageAsync(package, CancellationToken.None).Wait();
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
                packages = from
                                pkg in packages
                           where
                                ((pkg.Id == criteria.SearchTerm || pkg.Title == criteria.SearchTerm).Boost(4) ||
                                (pkg.SearchTitle == criteria.SearchTerm).Boost(3) ||
                                (pkg.Tags == criteria.SearchTerm).Boost(2) ||
                                (pkg.Authors.Contains(criteria.SearchTerm) || pkg.Owners.Contains(criteria.SearchTerm)).Boost(2) ||
                                (pkg.Files.Contains(criteria.SearchTerm)) ||
                                (pkg.Summary == criteria.SearchTerm || pkg.Description == criteria.SearchTerm)).AllowSpecialCharacters()
                           select
                               pkg;
            }

            if (!criteria.AllowPrereleaseVersions)
            {
                packages = packages.Where(p => !p.IsPrerelease);
            }

            if (criteria.PackageOriginFilter != PackageOriginFilter.Any)
            {
                var flag = criteria.PackageOriginFilter == PackageOriginFilter.Mirror;
                packages = packages.Where(p => p.IsMirrored == flag);
            }

            packages = ApplySort(criteria, packages);

            return packages;
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

            return criteria.SortDirection == SearchSortDirection.Ascending
                    ? packages.OrderBy(sortSelector)
                    : packages.OrderByDescending(sortSelector);
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

                if (versionConstraintList.Any() && versionConstraintList[ii] != null)
                {
                    matchedPackages = matchedPackages.Where(pkg => versionConstraintList[ii].Satisfies(pkg.Version.SemanticVersion));
                }
                else
                {
                    matchedPackages = matchedPackages.Where(pkg => pkg.Version > currentVersion);
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

        public Task SynchronizeWithFileSystem(CancellationToken cancellationToken)
        {
            Log.Info(m => m("Synchronizing packages with filesystem."));
            return Indexer.SynchronizeIndexWithFileSystemAsync(cancellationToken);
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
            return GetPackageFilePath((IPackageName)package);
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
            return FastZipPackage.Open(path, HashProvider);
        }

        public LucenePackage Convert(IPackage package)
        {
            var lucenePackage = package as LucenePackage;
            if (lucenePackage != null) return lucenePackage;

            lucenePackage = new LucenePackage(FileSystem);

            return Convert(package, lucenePackage);
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
                CalculateDerivedDataSlowlyConsumingLotsOfMemory(package, openStream);
            }
            else
            {
                package.PackageSize = fastPackage.Size;
                package.PackageHash = System.Convert.ToBase64String(fastPackage.Hash);
                package.Created = fastPackage.Created;
            }

            package.PackageHashAlgorithm = HashAlgorithm;
            package.LastUpdated = GetLastModified(package, path);
            package.Published = package.LastUpdated;
            package.Path = path;
            package.SupportedFrameworks = sourcePackage.GetSupportedFrameworks().Select(VersionUtility.GetShortFrameworkName);
            package.Files = sourcePackage.GetFiles().Select(f => f.Path).ToArray();
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

        private void CalculateDerivedDataSlowlyConsumingLotsOfMemory(LucenePackage package, Func<Stream> openStream)
        {
            byte[] fileBytes;
            using (var stream = openStream())
            {
                fileBytes = stream.ReadAllBytes();
            }

            package.PackageSize = fileBytes.Length;
            package.PackageHash = System.Convert.ToBase64String(HashProvider.CalculateHash(fileBytes));
            package.Created = GetZipArchiveCreateDate(new MemoryStream(fileBytes));
        }

        private DateTimeOffset GetZipArchiveCreateDate(Stream stream)
        {
            var zip = new ZipFile(stream);

            return zip.Cast<ZipEntry>()
                .Where(f => f.Name.EndsWith(".nuspec"))
                .Select(f => f.DateTime)
                .FirstOrDefault();
        }
    }
}
