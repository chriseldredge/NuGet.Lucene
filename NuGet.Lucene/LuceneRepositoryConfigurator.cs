using System;
using System.IO;
using System.Linq;
using Common.Logging;
using Lucene.Net.Index;
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

        protected PackageIndexer PackageIndexer { get; set; }

        protected PackageFileSystemWatcher PackageFileSystemWatcher { get; set; }

        protected IndexWriter IndexWriter { get; set; }

        protected LuceneDirectory LuceneDirectory { get; set; }

        protected LuceneDataProvider Provider { get; set; }

        public LuceneRepositoryConfigurator()
        {
            GroupPackageFilesById = true;
            PackageHashAlgorithm = "SHA512";
        }

        public void Initialize()
        {
            var packagePathResolver = CreatePackagePathResolver();
            var fileSystem = new PhysicalFileSystem(PackagePath);
            var hashProvider = new CryptoHashProvider(PackageHashAlgorithm);

            CreateDirectories();
            InitializeLucene();
            
            PackageIndexer = new PackageIndexer
                {
                    FileSystem = fileSystem,
                    Provider = Provider,
                    Writer = IndexWriter
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
            IndexWriter.Dispose();
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

            var analyzer = new PackageAnalyzer();
            var create = ShouldCreateIndex(LuceneDirectory);

            IndexWriter = new IndexWriter(LuceneDirectory, analyzer, create, IndexWriter.MaxFieldLength.UNLIMITED);
            Provider = new LuceneDataProvider(LuceneDirectory, analyzer, Version.LUCENE_30, IndexWriter);
        }
        
        private IPackagePathResolver CreatePackagePathResolver()
        {
            if (GroupPackageFilesById)
            {
                return new OneFolderPerPackageIdPathResolver(PackagePath);
            }

            return new DefaultPackagePathResolver(PackagePath);
        }

        private static bool ShouldCreateIndex(LuceneDirectory dir)
        {
            bool create;

            try
            {
                create = !dir.ListAll().Any();
            }
            catch (NoSuchDirectoryException)
            {
                create = true;
            }

            return create;
        }

        private static LuceneDirectory OpenLuceneDirectory(string luceneIndexPath)
        {
            var directoryInfo = new DirectoryInfo(luceneIndexPath);
            return FSDirectory.Open(directoryInfo, new NativeFSLockFactory(directoryInfo));
        }
    }
}