using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using ICSharpCode.SharpZipLib.Zip;
using Lucene.Net.Linq;
using NuGet.Lucene.Util;
using LuceneDirectory = Lucene.Net.Store.Directory;

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

        public async Task AddPackageAsync(IPackage package)
        {
            Log.Info(m => m("Adding package {0} {1} to file system", package.Id, package.Version));

            base.AddPackage(package);

            Log.Info(m => m("Indexing package {0} {1}", package.Id, package.Version));

            await Indexer.AddPackage(Convert(package));
        }

        public override void AddPackage(IPackage package)
        {
            var task = Task.Run(() => AddPackageAsync(package));
            task.Wait();
        }

        public async Task IncrementDownloadCount(IPackage package)
        {
            await Indexer.IncrementDownloadCount(package);
        }

        private void UpdatePackageCount(IQueryable<LucenePackage> packages)
        {
            packageCount = packages.Count();

            Log.Info(m => m("Refreshing index. Package count: " + packageCount));
        }

        public async Task RemovePackageAsync(IPackage package)
        {
            base.RemovePackage(package);
            
            await Indexer.RemovePackage(package);
        }

        public override void RemovePackage(IPackage package)
        {
            var task = Task.Run(() => RemovePackageAsync(package));
            
            task.Wait();
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return LucenePackages;
        }

        public override IPackage FindPackage(string packageId, SemanticVersion version)
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
            var packages = LucenePackages;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                packages = from
                                pkg in packages
                           where
                                ((pkg.Id == searchTerm || pkg.Title == searchTerm).Boost(3) ||
                                (pkg.Tags == searchTerm).Boost(2) ||
                                (pkg.Authors.Contains(searchTerm) || pkg.Owners.Contains(searchTerm)).Boost(2) ||
                                (pkg.Summary == searchTerm || pkg.Description == searchTerm)).AllowSpecialCharacters()
                           select
                               pkg;
            }

            if (!allowPrereleaseVersions)
            {
                packages = packages.Where(p => !p.IsPrerelease);
            }
            
            return packages;
        }

        public IEnumerable<IPackage> GetUpdates(IEnumerable<IPackage> packages, bool includePrerelease, bool includeAllVersions,
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
                    matchedPackages = matchedPackages.Where(pkg => pkg.GetSupportedFrameworks().Any(fwk => fwk == targetFrameworkList[ii]));
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
            return Indexer.SynchronizeIndexWithFileSystem(cancellationToken);
        }

        public RepositoryInfo GetStatus()
        {
            return new RepositoryInfo(packageCount, Indexer.GetIndexingStatus());
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
            var package = Convert(OpenPackage(fullPath), new LucenePackage(_ => FileSystem.OpenFile(path)));
            package.Path = FileSystem.MakeRelative(path);
            return package;
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

            var path = GetPackageFilePath(lucenePackage);
            lucenePackage.Path = path;

            CalculateDerivedData(package, lucenePackage, path, lucenePackage.GetStream());

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
        }

        private Uri FilterPlaceholderUri(Uri uri)
        {
            if (uri != null && uri.IsAbsoluteUri && uri.Host.ToLowerInvariant().Contains("url_here_or_delete_this_line"))
            {
                return null;
            }

            return uri;
        }

        protected virtual void CalculateDerivedData(IPackage sourcePackage, LucenePackage package, string path, Stream stream)
        {
            var fastPackage = sourcePackage as FastZipPackage;
            if (fastPackage == null)
            {
                CalculateDerivedDataSlowlyConsumingLotsOfMemory(package, stream);
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

            var localPackage = sourcePackage as LocalPackage;
            if (localPackage != null)
            {
                package.Files = localPackage.GetFiles().Select(f => f.Path);
            }
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

        private void CalculateDerivedDataSlowlyConsumingLotsOfMemory(LucenePackage package, Stream stream)
        {
            byte[] fileBytes;
            using (stream)
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