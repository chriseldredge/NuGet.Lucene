using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class PackageIndexerAddPackageTests : PackageIndexerTestBase
    {
        [SetUp]
        public void InitializeIndexer()
        {
            indexer.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            indexer.Dispose();
        }

        [Test]
        public async void AddPackage()
        {
            await indexer.AddPackage(MakeSamplePackage("Sample.Package", "1.0"));

            Assert.AreEqual(1, datasource.Count());
        }
        
        [Test]
        public async void AddPackage_SetIsLatestVersion()
        {
            await indexer.AddPackage(MakeSamplePackage("Sample.Package", "1.0"));

            Assert.True(datasource.First().IsLatestVersion, "IsLatestVersion");
        }

        [Test]
        public void AddPackage_MultipleVersions_UnsetIsLatestVersion()
        {
            var t1 = indexer.AddPackage(MakeSamplePackage("Sample.Package", "1.0"));
            var t2 = indexer.AddPackage(MakeSamplePackage("Sample.Package", "1.1"));

            Task.WaitAll(t1, t2);

            var packages = datasource.OrderBy(p => p.Version).ToArray();
            Assert.False(packages.First().IsLatestVersion, "older.IsLatestVersion");
            Assert.True(packages.Last().IsLatestVersion, "newer.IsLatestVersion");
        }

        [Test]
        public async void AddPackage_Replaces()
        {
            InsertPackage("Sample.Package", "1.0");

            await indexer.AddPackage(MakeSamplePackage("Sample.Package", "1.0"));

            Assert.AreEqual(1, datasource.Count());
        }

        [Test]
        public async void AddPackage_ReplacePreservesVersionDownloadCount()
        {
            const int versionDownloadCount = 199;
            var package = MakeSamplePackage("Sample.Package", "1.0");
            package.VersionDownloadCount = versionDownloadCount;
            InsertPackage(package);

            await indexer.AddPackage(package);

            Assert.AreEqual(versionDownloadCount, datasource.First().VersionDownloadCount);
        }

        [Test]
        public async void AddPackage_ReplacePreservesDownloadCount()
        {
            const int downloadCount = 23999;
            var package = MakeSamplePackage("Sample.Package", "1.0");
            package.DownloadCount = downloadCount;
            InsertPackage(package);

            await indexer.AddPackage(package);

            Assert.AreEqual(downloadCount, datasource.First().DownloadCount);
        }

        [Test]
        public void AddPackage_NewVersion_ZerosVersionDownloadCount()
        {
            var t1 = indexer.AddPackage(MakeSamplePackage("Sample.Package", "1.0"));
            var t2 = indexer.AddPackage(MakeSamplePackage("Sample.Package", "1.1"));

            Task.WaitAll(t1, t2);

            var packages = datasource.OrderBy(p => p.Version).ToArray();
            Assert.AreEqual(0, packages.Last().VersionDownloadCount);
        }

    }
}
