using System.Linq;
using System.Net.Http;
using AspNet.WebApi.HtmlMicrodataFormatter;
using NUnit.Framework;
using NuGet.Lucene.Web.Controllers;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class NuGetDocumentationControllerTests : ApiControllerTests<NuGetDocumentationController>
    {
        [SetUp]
        public void SetUp()
        {
            SetUpRequest(RouteNames.Indexing, HttpMethod.Get, "api/indexing/status");
        }

        protected override NuGetDocumentationController CreateController()
        {
            return new NuGetDocumentationController { NuGetWebApiRouteMapper = new NuGetWebApiRouteMapper("api/"), DocumentationProvider = new WebApiHtmlDocumentationProvider(new HtmlDocumentation())};
        }

        [Test]
        public void OData()
        {
            var all = controller.GetApiDocumentation();

            var match = all["Packages"].Actions.FirstOrDefault(i => i.Name == "OData");

            Assert.That(match, Is.Not.Null, "Api named OData");
        }

    }
}