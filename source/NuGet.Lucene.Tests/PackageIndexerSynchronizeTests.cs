using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Linq;
using Lucene.Net.Search;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class PackageIndexerSynchronizeTests : PackageIndexerTestBase
    {
        private static readonly string[] Empty = new string[0];
        private Mock<ISession<LucenePackage>> session;

        [SetUp]
        public void SetUp()
        {
            session = new Mock<ISession<LucenePackage>>();
            session.Setup(s => s.Query()).Returns(datasource);

            indexer.FakeSession = session.Object;
            indexer.Initialize();
        }

        [Test]
        public void DoesNothingOnNoDifferences()
        {
            indexer.SynchronizeIndexWithFileSystem(new IndexDifferences(Empty, Empty, Empty), CancellationToken.None);

            session.Verify();
        }

        [Test]
        public void DeletesMissingPackages()
        {
            var missing = new[] {"A.nupkg", "B.nupkg"};

            var deletedTerms = new List<Term>();

            session.Setup(s => s.Delete(It.IsAny<Query[]>())).Callback((Query[] query) =>
                deletedTerms.AddRange(query.Cast<TermQuery>().Select(q => q.Term)));

            indexer.SynchronizeIndexWithFileSystem(new IndexDifferences(Empty, missing, Empty), CancellationToken.None);

            session.Verify(s => s.Commit(), Times.AtLeastOnce());

            Assert.That(deletedTerms, Is.EquivalentTo(new[] { new Term("Path", "A.nupkg"), new Term("Path", "B.nupkg") }));
        }

        [Test]
        public void AddsNewPackages()
        {
            var newPackages = new[] { "A.1.0.nupkg" };

            var pkg = MakeSamplePackage("A", "1.0");
            loader.Setup(l => l.LoadFromFileSystem(newPackages[0])).Returns(pkg);

            session.Setup(s => s.Add(It.IsAny<LucenePackage>())).Verifiable();

            session.Setup(s => s.Commit()).Verifiable();

            indexer.SynchronizeIndexWithFileSystem(new IndexDifferences(newPackages, Empty, Empty), CancellationToken.None);

            session.VerifyAll();
        }

        [Test]
        public void ContinuesOnException()
        {
            var newPackages = new[] { "A.1.0.nupkg", "B.1.0.nupkg" };

            var pkg = MakeSamplePackage("B", "1.0");

            loader.Setup(l => l.LoadFromFileSystem(newPackages[0])).Throws(new Exception("invalid package"));
            loader.Setup(l => l.LoadFromFileSystem(newPackages[1])).Returns(pkg);

            session.Setup(s => s.Add(It.IsAny<LucenePackage>())).Verifiable();

            session.Setup(s => s.Commit()).Verifiable();

            indexer.SynchronizeIndexWithFileSystem(new IndexDifferences(newPackages, Empty, Empty), CancellationToken.None);

            session.VerifyAll();
        }
    }
}