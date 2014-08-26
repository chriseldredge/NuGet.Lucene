using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lucene.Net.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class PackageIndexerIncrementDownloadCountTests : PackageIndexerTestBase
    {
        private const string SampleId = "Sample.Package";
        private const string OtherSampleId = "Other.Package";
        private const string SampleVersion1 = "1.1";
        private const string SampleVersion2 = "2.0";

        [SetUp]
        public void InsertPackages()
        {
            InsertPackage(SampleId, SampleVersion1);
            InsertPackage(SampleId, SampleVersion2);
        }

        [Test]
        public void Apply_IncrementDownloadCountOnAllPackageVersions()
        {
            using (var session = OpenSession())
            {
                indexer.ApplyPendingDownloadIncrements(new List<LucenePackage> { MakeSamplePackage(SampleId, SampleVersion1) }, session);
            }

            Assert.That(datasource.ToList().All(p => p.DownloadCount == 1));
        }

        [Test]
        public void Apply_LeavesOtherPackages()
        {
            InsertPackage(OtherSampleId, SampleVersion1);

            using (var session = OpenSession())
            {
                indexer.ApplyPendingDownloadIncrements(new List<LucenePackage> { MakeSamplePackage(SampleId, SampleVersion1) }, session);
            }

            Assert.AreEqual(0, GetPackage(OtherSampleId, SampleVersion1).DownloadCount);
            Assert.AreEqual(0, GetPackage(OtherSampleId, SampleVersion1).VersionDownloadCount);
        }

        [Test]
        public void Apply_IncrementVersionDownloadCountOnlyOnSamePackageVersion()
        {
            using (var session = OpenSession())
            {
                indexer.ApplyPendingDownloadIncrements(new List<LucenePackage> { MakeSamplePackage(SampleId, SampleVersion2) }, session);
            }
            Assert.AreEqual(0, GetPackage(SampleId, SampleVersion1).VersionDownloadCount);
            Assert.AreEqual(1, GetPackage(SampleId, SampleVersion2).VersionDownloadCount);
        }

        [Test]
        public void Apply_IncrementByThree()
        {
            var package = MakeSamplePackage(SampleId, SampleVersion2);
            using (var session = OpenSession())
            {
                indexer.ApplyPendingDownloadIncrements(new List<LucenePackage> {package, package, package}, session);
            }
            Assert.AreEqual(0, GetPackage(SampleId, SampleVersion1).VersionDownloadCount);
            Assert.AreEqual(3, GetPackage(SampleId, SampleVersion1).DownloadCount);
            Assert.AreEqual(3, GetPackage(SampleId, SampleVersion2).DownloadCount);
            Assert.AreEqual(3, GetPackage(SampleId, SampleVersion2).VersionDownloadCount);
        }

        private ISession<LucenePackage> OpenSession()
        {
            return provider.OpenSession(() => new LucenePackage(fileSystem.Object));
        }

        [Test]
        public void IncrementQueuesForLater()
        {
            var task = indexer.IncrementDownloadCountAsync(MakeSamplePackage(SampleId, SampleVersion1), CancellationToken.None);
            Assert.AreEqual(task.IsCompleted, false);
        }

        [Test]
        public void IncrementThrowsOnBlankId()
        {
            Assert.Throws<InvalidOperationException>(() => indexer.IncrementDownloadCountAsync(MakeSamplePackage("", SampleVersion1), CancellationToken.None));
        }

        [Test]
        public void IncrementThrowsOnNullId()
        {
            Assert.Throws<InvalidOperationException>(() => indexer.IncrementDownloadCountAsync(MakeSamplePackage(null, SampleVersion1), CancellationToken.None));
        }

        [Test]
        public void IncrementThrowsOnNullVersion()
        {
            Assert.Throws<InvalidOperationException>(() => indexer.IncrementDownloadCountAsync(MakeSamplePackage(SampleId, null), CancellationToken.None));
        }

        private LucenePackage GetPackage(string id, string version)
        {
            return datasource.First(p => p.Id == id && p.Version == new StrictSemanticVersion(version));
        }
    }
}
