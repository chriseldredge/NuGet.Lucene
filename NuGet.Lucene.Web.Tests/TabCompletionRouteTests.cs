using System.Net.Http;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    [TestFixture]
    public class TabCompletionRouteTests : RouteTests
    {
        [Test]
        [TestCase("api/v2/package-ids")]
        public void PackageIds(string uri)
        {
            Assert.That(routes, HasRouteFor(uri, HttpMethod.Get)
                                    .WithController("TabCompletion")
                                    .WithAction("GetMatchingPackages"));
        }

        [Test]
        [TestCase("api/v2/package-versions/myPackage", "myPackage")]
        public void PackageVersions(string uri, string packageId)
        {
            Assert.That(routes, HasRouteFor(uri, HttpMethod.Get)
                                    .WithController("TabCompletion")
                                    .WithAction("GetPackageVersions")
                                    .WithRouteValue("packageId", packageId));
        }
    }
}