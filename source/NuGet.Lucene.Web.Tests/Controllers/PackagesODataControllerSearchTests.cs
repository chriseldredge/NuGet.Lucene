using System.Linq;
using System.Threading.Tasks;
using NuGet.Lucene.Tests;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesODataControllerSearchTests : PackagesODataControllerTestBase
    {
        [Test]
        public void SortsByScore()
        {
            var packages = new LucenePackage[0];
            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable()).Verifiable();
            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()");
            
            var query = controller.Search("foo", "", includePrerelease: false, options: queryOptions);

            repo.VerifyAll();

            AssertOrderingBy(query, "result => result.Score()");
        }

        [Test]
        public void NoSortWhenOrderSpecified()
        {
            var packages = new LucenePackage[0];
            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable()).Verifiable();
            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$orderby=Id");

            var query = controller.Search("foo", "", includePrerelease: false, options: queryOptions);

            repo.VerifyAll();

            AssertOrderingBy(query);
        }


        [Test]
        public async Task CountSearch()
        {
            var packages = new [] { new TestPackage("a", "1.0")};
            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable()).Verifiable();
            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$orderby=Id");

            var response = controller.CountSearch("foo", "", false, queryOptions);

            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(packages.Count().ToString()));
        }
    }
}
