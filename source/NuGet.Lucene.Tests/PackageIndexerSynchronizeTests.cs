using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task DoesNothingOnNoDifferences()
        {
            await indexer.SynchronizeIndexWithFileSystemAsync(new IndexDifferences(Empty, Empty, Empty), CancellationToken.None);

            session.Verify();
        }

        [Test]
        public async Task DeletesMissingPackages()
        {
            var missing = new[] {"A.nupkg", "B.nupkg"};

            var deletedTerms = new List<Term>();

            session.Setup(s => s.Delete(It.IsAny<Query[]>())).Callback((Query[] query) =>
                deletedTerms.AddRange(query.Cast<TermQuery>().Select(q => q.Term)));

            await indexer.SynchronizeIndexWithFileSystemAsync(new IndexDifferences(Empty, missing, Empty), CancellationToken.None);

            session.Verify(s => s.Commit(), Times.AtLeastOnce());

            Assert.That(deletedTerms, Is.EquivalentTo(new[] { new Term("Path", "A.nupkg"), new Term("Path", "B.nupkg") }));
        }

        [Test]
        public async Task AddsNewPackages()
        {
            var newPackages = new[] { "A.1.0.nupkg" };

            var pkg = MakeSamplePackage("A", "1.0");
            loader.Setup(l => l.LoadFromFileSystem(newPackages[0])).Returns(pkg);
            
            session.Setup(s => s.Add(KeyConstraint.None, It.IsAny<LucenePackage>())).Verifiable();

            session.Setup(s => s.Commit()).Verifiable();

            await indexer.SynchronizeIndexWithFileSystemAsync(new IndexDifferences(newPackages, Empty, Empty), CancellationToken.None);

            session.VerifyAll();
        }

        [Test]
        public async Task ContinuesOnException()
        {
            var newPackages = new[] { "A.1.0.nupkg", "B.1.0.nupkg" };

            var pkg = MakeSamplePackage("B", "1.0");

            loader.Setup(l => l.LoadFromFileSystem(newPackages[0])).Throws(new Exception("invalid package"));
            loader.Setup(l => l.LoadFromFileSystem(newPackages[1])).Returns(pkg);

            session.Setup(s => s.Add(KeyConstraint.None, It.IsAny<LucenePackage>())).Verifiable();

            session.Setup(s => s.Commit()).Verifiable();

            try
            {
                await
                    indexer.SynchronizeIndexWithFileSystemAsync(new IndexDifferences(newPackages, Empty, Empty),
                        CancellationToken.None);
            }
            catch (Exception ex)
            {
                Assert.That(ex.Message, Is.EqualTo("invalid package"));
            }

            session.VerifyAll();
        }
    }
}
