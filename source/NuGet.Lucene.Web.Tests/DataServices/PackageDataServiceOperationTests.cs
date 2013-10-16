using System;
using System.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.DataServices
{
    [TestFixture]
    public class PackageDataServiceOperationTests : PackageDataServiceTestBase
    {
        [Test]
        public void IsQueryForSpecificPackageMatch()
        {
            service.FakeRequestUri = new Uri("http://localhost:40223/api/odata/Packages(Id%3D'SharpZipLib'%2CVersion%3D'0.86.0.0')");
            string id;
            SemanticVersion version;
            
            var result = service.IsQueryForSpecificPackage(out id, out version);

            Assert.That(result, Is.True, "return value");
            Assert.That(id, Is.EqualTo("SharpZipLib"));
            Assert.That(version.ToString(), Is.EqualTo("0.86.0.0"));
        }

        [Test]
        public void IsQueryForSpecificPackageNoMatchWithoutVersion()
        {
            service.FakeRequestUri = new Uri("http://localhost:40223/api/odata/Packages(Id%3D'SharpZipLib')");
            string id;
            SemanticVersion version;

            var result = service.IsQueryForSpecificPackage(out id, out version);

            Assert.That(result, Is.False, "return value");
        }

        [Test]
        public void IsQueryForSpecificPackageNoMatchWithExtraJunk()
        {
            service.FakeRequestUri = new Uri("http://localhost:40223/api/odata/Packages(Id%3D'SharpZipLib'%2CVersion%3D'0.86.0.0')?extrajunk=true");
            string id;
            SemanticVersion version;

            var result = service.IsQueryForSpecificPackage(out id, out version);

            Assert.That(result, Is.False, "return value");
        }
    }
}