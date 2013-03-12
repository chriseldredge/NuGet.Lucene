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

        public override async void AddPackage(IPackage package)
        {
            await AddPackageAsync(package);
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
            await Task.Run(() => base.RemovePackage(package)).ContinueWith(_ => Indexer.RemovePackage(package));
        }

        public override async void RemovePackage(IPackage package)
        {
            await RemovePackageAsync(package);
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

        public IEnumerable<IPackage> GetUpdates(IEnumerable<IPackage> packages, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> targetFramework)
        {
            //TODO: could this be optimized?
            return this.GetUpdatesCore(packages, includePrerelease, includeAllVersions, targetFramework);
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
            var package = Convert(OpenPackage(path), new LucenePackage(_ => FileSystem.OpenFile(path)));
            package.Path = FileSystem.MakeRelative(path);
            return package;
        }

        public LucenePackage Convert(IPackage package)
        {
            if (package is LucenePackage) return (LucenePackage)package;

            var lucenePackage = new LucenePackage(FileSystem);

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
            lucenePackage.Title = package.Title;
            lucenePackage.Authors = (package.Authors ?? Enumerable.Empty<string>()).Select(i => i.Trim()).ToArray();
            lucenePackage.Owners = (package.Owners ?? Enumerable.Empty<string>()).Select(i => i.Trim()).ToArray();
            lucenePackage.IconUrl = package.IconUrl;
            lucenePackage.LicenseUrl = package.LicenseUrl;
            lucenePackage.ProjectUrl = package.ProjectUrl;
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
        }

        protected virtual void CalculateDerivedData(IPackage sourcePackage, LucenePackage package, string path, Stream stream)
        {
            byte[] fileBytes;
            using (stream)
            {
                fileBytes = stream.ReadAllBytes();
            }

            package.PackageSize = fileBytes.Length;
            package.PackageHash = System.Convert.ToBase64String(HashProvider.CalculateHash(fileBytes));
            package.PackageHashAlgorithm = HashAlgorithm;
            package.LastUpdated = FileSystem.GetLastModified(path);
            package.Published = package.LastUpdated;
            package.Created = GetZipArchiveCreateDate(new MemoryStream(fileBytes));
            package.Path = path;

            package.SupportedFrameworks = sourcePackage.GetSupportedFrameworks().Select(VersionUtility.GetShortFrameworkName);

            var localPackage = sourcePackage as LocalPackage;
            if (localPackage != null)
            {
                package.Files = localPackage.GetFiles().Select(f => f.Path);
            }
        }

        private DateTimeOffset GetZipArchiveCreateDate(Stream stream)
        {
            var f = new ZipFile(stream);
            foreach (ZipEntry file in f)
            {
                if (file.Name.EndsWith(".nuspec"))
                {
                    return file.DateTime;
                }
            }

            return DateTimeOffset.MinValue;
        }
    }
}