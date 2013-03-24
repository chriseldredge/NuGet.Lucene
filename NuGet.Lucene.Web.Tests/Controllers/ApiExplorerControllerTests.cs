using System.Linq;
using System.Net;
using System.Net.Http;
using Moq;
using NUnit.Framework;
using NuGet.Lucene.Web.Controllers;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class ApiExplorerControllerTests : ApiControllerTests<ApiExplorerController>
    {
        [SetUp]
        public void SetUp()
        {
            SetUpRequest(RouteNames.Indexing, HttpMethod.Get, "api/indexing/status");
        }

        protected override ApiExplorerController CreateController()
        {
            return new ApiExplorerController();
        }

        [Test]
        public void OData()
        {
            var all = controller.GetApiMethods();

            var match = all.FirstOrDefault(i => i.Name == "OData");

            Assert.That(match, Is.Not.Null, "Api named OData");
        }

    }
}