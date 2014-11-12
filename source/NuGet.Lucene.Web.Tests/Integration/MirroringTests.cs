using System;
using System.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Integration
{
    [TestFixture]
    internal class MirroringTests : IntegrationTestBase
    {
        [Test]
        public void ShowsVersionsFromMirror()
        {
            var repo = new DataServicePackageRepository(new Uri(ServerUrl + "api/odata/"));

            var result = repo.FindPackagesById("Nuget.Core");

            Assert.That(result.Count(), Is.GreaterThan(1), "Should look in mirror for packages.");
        }

        [Test]
        public void FindPackageMirrors()
        {
            var repo = new DataServicePackageRepository(new Uri(ServerUrl + "api/odata/"));

            var result = repo.FindPackage("Nuget.Core", new SemanticVersion("2.8.1"));

            Assert.That(result, Is.Not.Null, "Should mirror package from origin.");
            Assert.That(luceneRepository.LucenePackages.Count(), Is.EqualTo(1));
        }
    }
}
