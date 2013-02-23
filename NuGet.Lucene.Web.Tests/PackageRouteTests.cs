using System.Linq;
using System.Net.Http;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    [TestFixture]
    public class PackageRouteTests : RouteTests
    {
        [Test]
        [TestCase("api/v2/package/Sample/1.0", "1.0")]
        public void DeletePackage(string uri, string version)
        {
            Assert.That(routes, HasRouteFor(uri, HttpMethod.Delete)
                .WithController("Packages")
                .WithRouteValue("id", "Sample")
                .WithRouteValue("version", version));
        }

        [Test]
        [TestCase("api/v2/package/Sample/content", null)]
        [TestCase("api/v2/package/Sample/1.0/content", "1.0")]
        public void DownloadPackage(string uri, object version)
        {
            Assert.That(routes, HasRouteFor(uri)
                .WithController("Packages")
                .WithRouteValue("id", "Sample")
                .WithRouteValue("version", version));
        }

        [Test]
        [TestCase("api/v2/package", "Put")]
        [TestCase("api/v2/package", "Post")]
        public void UploadPackage(string uri, string method)
        {
            var httpMethod = (HttpMethod)typeof(HttpMethod).GetProperties().First(p => p.Name == method).GetValue(null);

            Assert.That(routes, HasRouteFor(uri, httpMethod)
                .WithController("Packages"));
        }

        [Test]
        [TestCase("api/v2/package/Sample", "")]
        [TestCase("api/v2/package/Sample/0.9", "0.9")]
        public void GetPackageInfo(string uri, object version)
        {
            Assert.That(routes, HasRouteFor(uri)
                .WithController("Packages")
                .WithAction("GetPackageInfo")
                .WithRouteValue("id", "Sample")
                .WithRouteValue("version", version));
        }

    }
}
