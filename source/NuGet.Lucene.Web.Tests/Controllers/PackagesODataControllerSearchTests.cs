using System.Linq;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using NuGet.Lucene.Web.Models;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesODataControllerSearchTests : PackagesODataControllerTestBase
    {
        protected ODataQueryOptions<ODataPackage> SetUpRequestWithOptions(string path)
        {
            SetUpRequest(RouteNames.Packages.Feed, HttpMethod.Post, path);
            return new ODataQueryOptions<ODataPackage>(new ODataQueryContext(model, typeof(ODataPackage)), request);
        }

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

    }
}
