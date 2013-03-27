using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class PackageFileSystemWatcherTests : TestBase
    {
        private Mock<ILog> log;
        private Mock<IPackageIndexer> indexer;
        private PackageFileSystemWatcher watcher;

        [SetUp]
        public void SetUp()
        {
            log = new Mock<ILog>();

            indexer = new Mock<IPackageIndexer>(MockBehavior.Strict);

            watcher = new PackageFileSystemWatcher
                          {
                              FileSystem = fileSystem.Object,
                              Indexer = indexer.Object,
                              PackageRepository = loader.Object,
                              QuietTime = TimeSpan.Zero,
                              Log = log.Object
                          };
        }

        [Test]
        public async Task PackageModified()
        {
            SetupPackageIsModified("Sample.1.0.nupkg");
            
            await watcher.OnPackageModified(@".\Sample.1.0.nupkg");

            loader.Verify();
            indexer.Verify();
        }

        [Test]
        public async Task PackageModified_HandlesException()
        {
            var exception = new Exception("LoadFromIndex: mock error");

            loader.Setup(ld => ld.LoadFromIndex(@"Sample.1.0.nupkg")).Throws(exception);
            log.Setup(l => l.Error(exception)).Verifiable();

            await watcher.OnPackageModified(@"Sample.1.0.nupkg");

            loader.Verify();
            indexer.Verify();
            log.Verify();
        }

        [Test]
        public async Task PackageDeleted()
        {
            SetupDeletePackage("Sample.1.0.nupkg");

            await watcher.OnPackageDeleted(@".\Sample.1.0.nupkg");

            loader.Verify();
            indexer.Verify();
        }

        [Test]
        public async Task PackageDeleted_HandlesException()
        {
            var exception = new Exception("RemovePackage: mock error");
            var lucenePackage = new LucenePackage(fileSystem.Object);

            loader.Setup(ld => ld.LoadFromIndex(@"Sample.1.0.nupkg")).Returns(lucenePackage);
            indexer.Setup(idx => idx.RemovePackage(lucenePackage)).Throws(exception);
            log.Setup(l => l.Error(exception)).Verifiable();

            await watcher.OnPackageDeleted("Sample.1.0.nupkg");

            loader.Verify();
            indexer.Verify();
            log.Verify();
        }

        [Test]
        public async Task PackageDeleted_IgnoresPackageMissingFromIndex()
        {
            loader.Setup(ld => ld.LoadFromIndex(@"Sample.1.0.nupkg")).Returns((LucenePackage)null);

            await watcher.OnPackageDeleted("Sample.1.0.nupkg");

            loader.Verify();
            indexer.Verify();
        }

        [Test]
        public async Task PackageRenamed()
        {
            SetupDeletePackage("tmp.nupkg");
            SetupPackageIsModified("Sample.1.0.nupkg");

            await watcher.OnPackageRenamed(@".\tmp.nupkg", @".\Sample.1.0.nupkg");

            loader.Verify();
            indexer.Verify();
        }

        [Test]
        public async Task PackageRenamed_IgnoresNonPackageExtension()
        {
            SetupDeletePackage("Sample.1.0.nupkg");
            
            await watcher.OnPackageRenamed(@".\Sample.1.0.nupkg", @".\IgnoreMe.tmp");
            
            loader.Verify();
            indexer.Verify();
        }

        [Test]
        public void SynchronizeAfterDirectoryCreated()
        {
            const string dir = @"c:\sample\dir";

            fileSystem.Setup(fs => fs.GetFiles(dir, "*.nupkg", true)).Returns(new[] { "Sample.1.0.nupkg" });
            indexer.Setup(idx => idx.SynchronizeIndexWithFileSystem(CancellationToken.None));

            watcher.OnDirectoryMoved(Path.GetDirectoryName(dir));

            fileSystem.Verify();
            indexer.Verify();
        }

        [Test]
        public void DirectoryCreatedIgnoreEmptyDir()
        {
            const string dir = @"c:\sample\dir";

            fileSystem.Setup(fs => fs.GetFiles(dir, "*.nupkg", true)).Returns(new string[0]);

            watcher.OnDirectoryMoved(Path.GetDirectoryName(dir));

            fileSystem.Verify();
            indexer.Verify();
        }

        private void SetupPackageIsModified(string filename)
        {
            var lucenePackage = new LucenePackage(fileSystem.Object);
            loader.Setup(ld => ld.LoadFromIndex(@".\" + filename)).Returns((LucenePackage)null).Verifiable();
            loader.Setup(ld => ld.LoadFromFileSystem(@".\" + filename)).Returns(lucenePackage).Verifiable();
            indexer.Setup(idx => idx.AddPackage(lucenePackage)).Returns(Task.FromResult<object>(null)).Verifiable();
        }

        private void SetupPackageIsNotModified(string filename)
        {
            var lucenePackage = new LucenePackage(fileSystem.Object) { Published = null };

            loader.Setup(ld => ld.LoadFromIndex(@".\" + filename)).Returns(lucenePackage).Verifiable();
            loader.Setup(ld => ld.LoadFromFileSystem(@".\" + filename)).Returns(lucenePackage).Verifiable();
            indexer.Setup(idx => idx.AddPackage(lucenePackage)).Verifiable();
        }

        private void SetupDeletePackage(string filename)
        {
            var lucenePackage = new LucenePackage(fileSystem.Object);
            loader.Setup(ld => ld.LoadFromIndex(@".\" + filename)).Returns(lucenePackage).Verifiable();
            indexer.Setup(idx => idx.RemovePackage(lucenePackage)).Returns(Task.FromResult<object>(null)).Verifiable();
        }
    }

}