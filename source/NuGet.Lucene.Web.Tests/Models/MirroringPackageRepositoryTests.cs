using System.Linq;
using Moq;
using NUnit.Framework;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Tests.Models
{
    [TestFixture]
    public class MirroringPackageRepositoryTests
    {
        private IPackage package1;
        private IPackage package2;
        private Mock<IPackageLookup> mirror;
        private Mock<IPackageLookup> origin;
        private MirroringPackageRepository repo;

        [SetUp]
        public void SetUp()
        {
            mirror = new Mock<IPackageLookup>();
            origin = new Mock<IPackageLookup>();

            repo = new MirroringPackageRepository(mirror.Object, origin.Object);

            package1 = new DataServicePackage { Id = "FuTools", Version = "1.0" };
            package2 = new DataServicePackage {Id = "FuTools", Version = "2.0"};
        }

        [Test]
        public void FindPackagesDistinct()
        {
            var copyOfPackage1 = new PackageSpec(package1.Id, package1.Version.ToString());

            mirror.Setup(r => r.FindPackagesById("FuTools")).Returns(new[] { package1 }).Verifiable();
            origin.Setup(r => r.FindPackagesById("FuTools")).Returns(new[] { copyOfPackage1, package2 }).Verifiable();

            var result = repo.FindPackagesById("FuTools");

            mirror.VerifyAll();
            origin.VerifyAll();

            Assert.That(result.ToList(), Is.EqualTo(new[] {package1, package2}));
        }

        [Test]
        public void FindPackageInMirror()
        {
            mirror.Setup(r => r.FindPackage(package1.Id, package1.Version)).Returns(package1).Verifiable();

            var result = repo.FindPackage(package1.Id, package1.Version);

            Assert.That(result, Is.SameAs(package1));

            mirror.VerifyAll();
            origin.Verify(r => r.FindPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>()), Times.Never());
        }

        [Test]
        public void PackageNotFound()
        {
            mirror.Setup(r => r.FindPackage(package1.Id, package1.Version)).Returns((IPackage)null).Verifiable();
            origin.Setup(r => r.FindPackage(package1.Id, package1.Version)).Returns((IPackage)null).Verifiable();

            var result = repo.FindPackage(package1.Id, package1.Version);

            Assert.That(result, Is.Null);

            mirror.VerifyAll();
            origin.VerifyAll();
        }

        [Test]
        public void PackageInOriginAddedToMirror()
        {
            var addedPackage = (IPackage) null;

            mirror.Setup(r => r.FindPackage(package1.Id, package1.Version)).Returns(() => addedPackage);
            origin.Setup(r => r.FindPackage(package1.Id, package1.Version)).Returns(package1).Verifiable();
            mirror.Setup(r => r.AddPackage(package1)).Callback<IPackage>(pkg => addedPackage = new PackageSpec(pkg.Id, pkg.Version.ToString()));

            var result = repo.FindPackage(package1.Id, package1.Version);

            Assert.That(result, Is.SameAs(addedPackage));

            mirror.VerifyAll();
            origin.VerifyAll();
        }

        [Test]
        public void OverridesHttpClientSettings()
        {
            origin.Setup(r => r.FindPackage(package1.Id, package1.Version)).Returns(package1).Verifiable();

            var result = repo.FindPackageInOrigin(package1.Id, package1.Version) as DataServicePackage;

            Assert.That(result, Is.Not.Null, "Expected instance of DataServicePackage");

            //result.do
            origin.VerifyAll();
        }
    }
}
