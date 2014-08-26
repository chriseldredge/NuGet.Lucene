using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    public abstract class PackageIndexerTestBase : TestBase
    {
        protected TestablePackageIndexer indexer;

        [SetUp]
        public void CreateIndexer()
        {
            indexer = new TestablePackageIndexer
                          {
                              FileSystem = fileSystem.Object,
                              Provider = provider,
                              Writer = indexWriter,
                              PackageRepository = loader.Object
                          };
        }

        public class TestablePackageIndexer : PackageIndexer
        {
            public ISession<LucenePackage> FakeSession { get; set; }

            protected internal override ISession<LucenePackage> OpenSession()
            {
                return FakeSession ?? base.OpenSession();
            }

            public Task AddPackageAsync(LucenePackage makeSamplePackage)
            {
                return AddPackageAsync(makeSamplePackage, CancellationToken.None);
            }
        }
    }
}
