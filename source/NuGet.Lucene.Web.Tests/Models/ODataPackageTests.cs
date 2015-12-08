using NuGet.Lucene.Web.Models;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Models
{
    [TestFixture]
    public class ODataPackageTests
    {
        [Test]
        public void Equals()
        {
            var a1 = new ODataPackage(new DataServicePackage { Id = "a", Version = "1.0" });
            var a2 = new ODataPackage(new DataServicePackage { Id = "a", Version = "1.0" });

            Assert.That(a1, Is.EqualTo(a2));
        }

        [Test]
        public void EqualsIgnoresOtherProperties()
        {
            var a1 = new ODataPackage(new DataServicePackage { Id = "a", Version = "1.0", Description = "ignore me" });
            var a2 = new ODataPackage(new DataServicePackage { Id = "a", Version = "1.0", Description = "ignore me and me too" });

            Assert.That(a1, Is.EqualTo(a2));
        }

        [Test]
        public void DifferentIdNotEqual()
        {
            var a1 = new ODataPackage(new DataServicePackage { Id = "a", Version = "1.0" });
            var a2 = new ODataPackage(new DataServicePackage { Id = "b", Version = "1.0" });

            Assert.That(a1, Is.Not.EqualTo(a2));
        }

        [Test]
        public void DifferentVersionNotEqual()
        {
            var a1 = new ODataPackage(new DataServicePackage { Id = "a", Version = "1.0" });
            var a2 = new ODataPackage(new DataServicePackage { Id = "a", Version = "2.0" });

            Assert.That(a1, Is.Not.EqualTo(a2));
        }

        [Test]
        public void SetsNormalizedVersionFromDataServicePackage()
        {
            var package = new ODataPackage(new DataServicePackage { Id = "a", Version = "1.0.0.0" });

            Assert.That(package.NormalizedVersion, Is.EqualTo("1.0.0"));
        }
    }
}
