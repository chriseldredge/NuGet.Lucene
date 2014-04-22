using System.Linq;
using System.Web.Http.Results;
using NuGet.Lucene.Web.Models;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesODataControllerFindPackagesByIdTests : PackagesODataControllerTestBase
    {
        [Test]
        public void SimpleCase()
        {
            var packages = new[] {new LucenePackage(path => null) {Id="MyPackage", Version = new StrictSemanticVersion("1.0")}};

            repo.Setup(r => r.FindPackagesById("MyPackage")).Returns(packages).Verifiable();
            
            var result = controller.FindPackagesById("MyPackage") as OkNegotiatedContentResult<IQueryable<ODataPackage>>;

            repo.Verify();

            Assert.That(result.Content.Count(), Is.EqualTo(1));
        }
    }
}