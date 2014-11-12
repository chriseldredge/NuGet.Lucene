using System;
using System.Linq;
using System.Net;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Integration
{
    [TestFixture]
    internal class PushTests : IntegrationTestBase
    {
        PackageServer server;
        long packageSize;
        IPackage package;

        [SetUp]
        public void PrepareSamplePackage()
        {
            package = LoadSamplePackage("Package", "1.0.0");
            
            using (var stream = package.GetStream())
            {
                packageSize = stream.Length;
            }

            server = new PackageServer(ServerUrl + "api/packages", "");
        }

        [Test]
        public void PushPackage()
        {
            server.PushPackage(
                WebServerSetUp.LocalAdministratorApiKey,
                package,
                packageSize,
                timeout:(int)TimeSpan.FromSeconds(30).TotalMilliseconds,
                disableBuffering:true);

            Assert.That(luceneRepository.LucenePackages.Count(), Is.EqualTo(1));
        }

        [Test]
        public void RejectsInvalidKey()
        {
            TestDelegate call = () => server.PushPackage(
                "invalid key",
                package,
                packageSize,
                timeout: (int)TimeSpan.FromSeconds(30).TotalMilliseconds,
                disableBuffering: false);

            var ex = Assert.Throws<InvalidOperationException>(call);
            var cause = (WebException)ex.InnerException;

            Assert.That(((HttpWebResponse)cause.Response).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
