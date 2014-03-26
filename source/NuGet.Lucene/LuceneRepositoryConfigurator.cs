using System;
using System.IO;
using Common.Logging;
using Lucene.Net.Linq;
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace NuGet.Lucene
{
    public class LuceneRepositoryConfigurator : IDisposable
    {
        /// <summary>
        /// Directory to hold nupkg files that will be indexed by Lucene.
        /// </summary>
        public string PackagePath { get; set; }

        /// <summary>
        /// Directory to hold Lucene index files.
        /// </summary>
        public string LuceneIndexPath { get; set; }

        /// <summary>
        /// Flag indicating if packages should be stored in subdirectories
        /// grouped by package ID or if they should all be stored in the
        /// top level directory. Defaults to <code>true</code>.
        /// </summary>
        public bool GroupPackageFilesById { get; set; }

        /// <summary>
        /// Flag that enables PackagePath to be monitored
        /// for file system events and to keep the Lucene
        /// index in sync when packages are created, modified
        /// or deleted. This functionality will keep the
        /// Lucene index up to date even when packages
        /// are changed on the file system without using
        /// the methods on LucenePackageRepository.
        /// Defaults to <code>false</code>.
        /// </summary>
        public bool EnablePackageFileWatcher { get; set; }

        /// <summary>
        /// Specify the cryptographic hashing algorithm
        /// to use for calculating package hashes.
        /// Defaults to SHA512.
        /// </summary>
        public string PackageHashAlgorithm { get; set; }

        /// <summary>
        /// Holds a reference to the configured package repository
        /// after <see cref="Initialize"/> has been invoked.
        /// </summary>
        public ILucenePackageRepository Repository { get; private set; }

        /// <summary>
        /// Holds a reference to the LINQ data provider after
        /// <see cref="Initialize"/> has been invoked.
        /// </summary>
        public LuceneDataProvider Provider { get; set; }

        /// <summary>
        /// Holds a reference to the Lucene Directory after
        /// <see cref="Initialize"/> has been invoked.
        /// </summary>
        public LuceneDirectory LuceneDirectory { get; set; }

        protected PackageIndexer PackageIndexer { get; set; }

        protected PackageFileSystemWatcher PackageFileSystemWatcher { get; set; }

        public LuceneRepositoryConfigurator()
        {
            GroupPackageFilesById = true;
            PackageHashAlgorithm = "SHA512";
        }

        public void Initialize()
        {
            var packagePathResolver = CreatePackagePathResolver();
            var fileSystem = new PhysicalFileSystem(PackagePath) { Logger = new NuGetCommonLoggingAdapter() };
            var hashProvider = new CryptoHashProvider(PackageHashAlgorithm);

            CreateDirectories();
            InitializeLucene();
            
            PackageIndexer = new PackageIndexer
                {
                    FileSystem = fileSystem,
                    Provider = Provider,
                    Writer = Provider.IndexWriter
                };

            var repository = new LucenePackageRepository(packagePathResolver, fileSystem)
                {
                    HashProvider = hashProvider,
                    HashAlgorithm = PackageHashAlgorithm,
                    PathResolver = packagePathResolver,
                    Indexer = PackageIndexer,
                    LuceneDataProvider = Provider,
                    LucenePackages = Provider.AsQueryable(() => new LucenePackage(fileSystem)),
                    LucenePackageSource = string.Format("{0} (with Lucene.Net index in {1})", PackagePath, LuceneIndexPath)
                };

            // TODO: circular reference
            PackageIndexer.PackageRepository = repository;

            PackageIndexer.Initialize();
            repository.Initialize();

            Repository = repository;

            InitializeFileSystemWatcher(fileSystem, repository);
        }

        public void Dispose()
        {
            LogManager.GetCurrentClassLogger().Info("Stopping Lucene indexing services.");

            if (PackageFileSystemWatcher != null)
            {
                PackageFileSystemWatcher.Dispose();
            }

            PackageIndexer.Dispose();
            Provider.Dispose();
            LuceneDirectory.Dispose();
        }

        private void CreateDirectories()
        {
            if (!Directory.Exists(LuceneIndexPath))
            {
                Directory.CreateDirectory(LuceneIndexPath);
            }
            if (!Directory.Exists(PackagePath))
            {
                Directory.CreateDirectory(PackagePath);
            }
        }

        private void InitializeFileSystemWatcher(IFileSystem fileSystem, ILucenePackageRepository repository)
        {
            if (!EnablePackageFileWatcher) return;

            PackageFileSystemWatcher = new PackageFileSystemWatcher
                {
                    FileSystem = fileSystem,
                    Indexer = PackageIndexer,
                    PackageRepository = repository
                };

            PackageFileSystemWatcher.Initialize();
        }
        
        private void InitializeLucene()
        {
            LuceneDirectory = OpenLuceneDirectory(LuceneIndexPath);

            Provider = new LuceneDataProvider(LuceneDirectory, Version.LUCENE_30);
            Provider.Settings.EnableMultipleEntities = false;
        }
        
        private IPackagePathResolver CreatePackagePathResolver()
        {
            return new GroupingPackagePathResolver(PackagePath, GroupPackageFilesById);
        }

        private static LuceneDirectory OpenLuceneDirectory(string luceneIndexPath)
        {
            var directoryInfo = new DirectoryInfo(luceneIndexPath);
            return FSDirectory.Open(directoryInfo, new NativeFSLockFactory(directoryInfo));
        }
    }
}