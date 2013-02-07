using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class LucenePackageRepositoryTests : TestBase
    {
        private Mock<IPackageIndexer> indexer;
        private LucenePackageRepository repository;

        [SetUp]
        public void SetUp()
        {
            indexer = new Mock<IPackageIndexer>();
            repository = new LucenePackageRepository(packagePathResolver.Object, fileSystem.Object)
                             {
                                 Indexer = indexer.Object,
                                 LucenePackages = datasource,
                                 LuceneDataProvider = provider,
                                 HashProvider = new CryptoHashProvider()
                             };
        }

        [Test]
        public void IncrementDownloadCount()
        {
            var pkg = MakeSamplePackage("sample", "2.1");
            indexer.Setup(i => i.IncrementDownloadCount(pkg)).Verifiable();

            repository.IncrementDownloadCount(pkg);

            indexer.Verify();
        }

        [Test]
        public void Initialize_UpdatesMaxDownload()
        {
            var p = MakeSamplePackage("a", "1.0");
            p.DownloadCount = 1234;
            repository.LucenePackages = new EnumerableQuery<LucenePackage>(new[] { p });

            repository.Initialize();

            Assert.That(repository.MaxDownloadCount, Is.EqualTo(p.DownloadCount));
        }

        [Test]
        public void FindPackage()
        {
            InsertPackage("a", "1.0");
            InsertPackage("a", "2.0");
            InsertPackage("b", "2.0");

            var result = repository.FindPackage("a", new SemanticVersion("2.0"));

            Assert.That(result.Id, Is.EqualTo("a"));
            Assert.That(result.Version.ToString(), Is.EqualTo("2.0"));
        }

        [Test]
        public void FindPackage_ExactMatch()
        {
            InsertPackage("a", "1.0");
            InsertPackage("a", "1.0.0.0");

            var result = repository.FindPackage("a", new SemanticVersion("1.0.0.0"));

            Assert.That(result.Id, Is.EqualTo("a"));
            Assert.That(result.Version.ToString(), Is.EqualTo("1.0.0.0"));
        }

        [Test]
        public void ConvertPackage_SupporteedFrameworks()
        {
            var package = SetUpConvertPackage();

            var result = repository.Convert(package.Object);

            Assert.That(result.SupportedFrameworks, Is.Not.Null, "SupportedFrameworks");
            Assert.That(result.SupportedFrameworks.ToArray(), Is.EquivalentTo(new[] {"net40"}));
        }

        [Test]
        public void ConvertPackage_Files()
        {
            var package = SetUpConvertPackage();
            var file1 = new Mock<IPackageFile>();
            file1.Setup(f => f.Path).Returns("path1");
            package.Object.Files = new[] {file1.Object};

            var result = repository.Convert(package.Object);

            Assert.That(result.Files, Is.Not.Null, "Files");
            Assert.That(result.Files.ToArray(), Is.EquivalentTo(new[] { "path1" }));
        }

        private Mock<PackageWithFiles> SetUpConvertPackage()
        {
            var package = new Mock<PackageWithFiles>();

            package.Object.Id = "Sample";
            package.Object.Version = new SemanticVersion("1.0");
            package.Object.DependencySets = new List<PackageDependencySet>();

            package.Setup(p => p.GetSupportedFrameworks()).Returns(new[] {VersionUtility.ParseFrameworkName("net40")});
            fileSystem.Setup(fs => fs.OpenFile(It.IsAny<string>())).Returns(new MemoryStream());
            package.Setup(p => p.GetStream()).Returns(new MemoryStream());
            return package;
        }

        public abstract class PackageWithFiles : LocalPackage
        {
            public IEnumerable<IPackageFile> Files { get;  set; }
            protected sealed override IEnumerable<IPackageFile> GetFilesBase()
            {
                return Files ?? new IPackageFile[0];
            }
        }
    }
}