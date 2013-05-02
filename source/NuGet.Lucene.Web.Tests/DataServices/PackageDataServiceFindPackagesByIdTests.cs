using System.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.DataServices
{
    [TestFixture]
    public class PackageDataServiceFindPackagesByIdTests : PackageDataServiceTestBase
    {
        [Test]
        public void SimpleCase()
        {
            var packages = new[] {new LucenePackage(path => null) {Id="MyPackage", Version = new StrictSemanticVersion("1.0")}};

            repo.Setup(r => r.FindPackagesById("MyPackage")).Returns(packages).Verifiable();
            
            var result = service.FindPackagesById("MyPackage");

            repo.Verify();

            Assert.That(result.Count(), Is.EqualTo(1));
        }
    }
}