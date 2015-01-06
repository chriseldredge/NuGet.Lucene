using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;
using Moq;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Symbols;
using NuGet.Lucene.Web.Util;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class SymbolsControllerTests : ApiControllerTests<SymbolsController>
    {
        private Mock<ISymbolSource> symbolSource;
        private Task completeTask;
        private static readonly StrictSemanticVersion SampleVersion = new StrictSemanticVersion("1.0");
        private LucenePackage package;
        private UrlHelper url;

        [SetUp]
        public void SetUp()
        {
            completeTask = new Task(() => { });
            completeTask.RunSynchronously();

            package = CreatePackage(SampleVersion);

            SetUpRequest(RouteNames.Symbols.Upload, HttpMethod.Put, "api/symbols");

            url = new UrlHelper(request);
        }

        protected override SymbolsController CreateController()
        {
            symbolSource = new Mock<ISymbolSource>();

            return new SymbolsController
            {
                SymbolSource = symbolSource.Object,
            };
        }

        [Test]
        public async Task PutPackage()
        {
            symbolSource.Setup(ss => ss.Enabled).Returns(true);
            symbolSource.Setup(ss => ss.AddSymbolsAsync(package, url.GetSymbolSourceUri())).Returns(completeTask);

            var result = await controller.PutPackage(package);

            symbolSource.Verify(ss => ss.AddSymbolsAsync(package, url.GetSymbolSourceUri()));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task PutPackageToolsNotAvailable()
        {
            symbolSource.Setup(ss => ss.Enabled).Returns(false);

            var result = await controller.PutPackage(package);

            symbolSource.Verify(ss => ss.AddSymbolsAsync(It.IsAny<IPackage>(), It.IsAny<string>()), Times.Never());
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotImplemented));
        }

        [Test]
        public async Task PutPackageCannotBeNull()
        {
            var result = await controller.PutPackage(null);

            symbolSource.Verify(ss => ss.AddSymbolsAsync(It.IsAny<IPackage>(), It.IsAny<string>()), Times.Never());
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutPackageMustContainSourceAndSymbols()
        {
            package.Files = new string[0];
            symbolSource.Setup(ss => ss.Enabled).Returns(true);

            var result = await controller.PutPackage(package);

            symbolSource.Verify(ss => ss.AddSymbolsAsync(It.IsAny<IPackage>(), It.IsAny<string>()), Times.Never());
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        private static LucenePackage CreatePackage(StrictSemanticVersion version)
        {
            return new LucenePackage(_ => new MemoryStream(Encoding.UTF8.GetBytes("<fake package contents>")))
            {
                Id = "Sample",
                Version = version,
                PackageHash = "fake hash",
                PackageSize = 12345678L,
                Created = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Published = new DateTimeOffset(3000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                LastUpdated = new DateTimeOffset(4000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Files = new[] { Path.Combine("src", "Class1.cs"), Path.Combine("lib", "net35", "Sample.PDB")}
            };
        }
    }
}
