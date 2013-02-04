using System.Web.Mvc;
using System.Web.Routing;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    [TestFixture]
    public class PackageRouteTests : RouteTests
    {
        private RouteCollection routes;

        [SetUp]
        public void BuildRouteTable()
        {
            routes = new RouteCollection();
            Global.MapMvcRoutes(routes);
            Global.MapApiRoutes(routes);
        }

        [Test]
        [TestCase("~/api/v2/package/Sample/1.0", "1.0")]
        public void DeletePackage(string uri, string version)
        {
            Assert.That(routes, HasRouteFor(uri, "delete")
                .WithController("Packages")
                .WithRouteValue("id", "Sample")
                .WithRouteValue("version", version));
        }

        [Test]
        [TestCase("~/api/v2/package/Sample", null)]
        [TestCase("~/api/v2/package/Sample/", null)]
        [TestCase("~/api/v2/package/Sample/1.0", "1.0")]
        [TestCase("~/api/v2/package/Sample/0.9/", "0.9")]
        public void DownloadPackage(string uri, object version)
        {
            Assert.That(routes, HasRouteFor(uri)
                .WithController("Packages")
                .WithRouteValue("id", "Sample")
                .WithRouteValue("version", version ?? UrlParameter.Optional));
        }

        [Test]
        [TestCase("~/api/v2/package", "put")]
        [TestCase("~/api/v2/package", "post")]
        public void UploadPackage(string uri, string method)
        {
            Assert.That(routes, HasRouteFor(uri, method)
                .WithController("Packages"));
        }

    }
}
