using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Integration
{
    [TestFixture]
    internal class SearchTests : IntegrationTestBase
    {
        [Test]
        public async Task Test()
        {
            await luceneRepository.AddPackageAsync(LoadSamplePackage("Package", "1.0.0"), CancellationToken.None);

            var repo = new DataServicePackageRepository(new Uri(ServerUrl + "api/odata/"));
            var results = repo.Search("", new string[0], allowPrereleaseVersions: false).ToList();
            Assert.That(results.Count, Is.EqualTo(1));
        }
    }
}
