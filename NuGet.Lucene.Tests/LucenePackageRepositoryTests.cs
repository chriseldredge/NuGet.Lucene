using System.Linq;
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
                                 LuceneDataProvider = provider
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
    }
}