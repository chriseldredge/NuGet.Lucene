using System.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesODataControllerSearchTests : PackagesODataControllerTestBase
    {
        [Test]
        public void SearchSortsByScore()
        {
            var packages = new LucenePackage[0];

            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable()).Verifiable();

            controller.Search("foo", "", includePrerelease: false);

            repo.VerifyAll();
        }
    }
}
