using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Moq;
using NUnit.Framework;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesControllerTests : ApiControllerTests<PackagesController>
    {
        private Mock<ILucenePackageRepository> luceneRepository;
        private Mock<IMirroringPackageRepository> mirroringRepository;
        private List<LucenePackage> packages;
        private Task completeTask;
        private static readonly StrictSemanticVersion SampleVersion = new StrictSemanticVersion("1.0");
        private LucenePackage package;

        [SetUp]
        public void SetUp()
        {
            packages = new List<LucenePackage>();

            completeTask = new Task(() => { });
            completeTask.RunSynchronously();

            package = CreatePackage(SampleVersion);

            SetUpRequest(RouteNames.Packages.Search, HttpMethod.Get, "api/packages");
        }

        protected override PackagesController CreateController()
        {
            luceneRepository = new Mock<ILucenePackageRepository>();
            mirroringRepository = new Mock<IMirroringPackageRepository>();

            return new PackagesController {LuceneRepository = luceneRepository.Object, MirroringRepository = mirroringRepository.Object};
        }

        [Test]
        public void GetPackageIdAndVersionNotFound()
        {
            package = null;

            var result = DownloadPackage(HttpMethod.Head, "SomePackage", SampleVersion.SemanticVersion);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void GetPackageIdNotFound()
        {
            package = null;

            var result = DownloadPackage(HttpMethod.Head, "SomePackage", null);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void GetLatestPackageVersionExcludesPreRelease()
        {
            packages.Add(CreatePackage(new StrictSemanticVersion("3.0-pre")));
            packages.Add(CreatePackage(new StrictSemanticVersion("2.0")));
            packages.Add(CreatePackage(new StrictSemanticVersion("1.0")));

            var result = DownloadPackage(HttpMethod.Head, "SomePackage", null);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result.Content.Headers.ContentDisposition.FileName, Is.EqualTo("Sample.2.0.nupkg"));
        }

        [Test]
        public void GetPackageHeaders()
        {
            var result = DownloadPackage(HttpMethod.Head, "SomePackage", SampleVersion.SemanticVersion);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result.Content.Headers.ContentType.MediaType, Is.EqualTo("application/zip"));
            Assert.That(result.Headers.ETag, Is.Not.Null, "ETag header");
            Assert.That(result.Headers.ETag.Tag, Is.EqualTo('"' + package.PackageHash + '"'));
            Assert.That(result.Headers.ETag.IsWeak, Is.False, "ETag.IsWeak");
            Assert.That(result.Content.Headers.ContentDisposition.DispositionType, Is.EqualTo("attachment"));
            Assert.That(result.Content.Headers.ContentDisposition.Size, Is.EqualTo(package.PackageSize));
            Assert.That(result.Content.Headers.ContentDisposition.ModificationDate, Is.EqualTo(package.LastUpdated));
            Assert.That(result.Content.Headers.ContentDisposition.CreationDate, Is.EqualTo(package.Created));
        }

        [Test]
        public async Task GetPackageHeadersSendsNoContent()
        {
            var result = DownloadPackage(HttpMethod.Head, "SomePackage", SampleVersion.SemanticVersion);

            Assert.That(await GetContent(result), Is.Empty);
        }

        [Test]
        public async Task GetPackageNotModifiedByEtag()
        {
            request.Headers.IfMatch.Add(new EntityTagHeaderValue('"' + package.PackageHash + '"'));

            var result = DownloadPackage(HttpMethod.Get, "SomePackage", SampleVersion.SemanticVersion);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(await GetContent(result), Is.Empty);
        }

        [Test]
        public async Task GetPackageNotModifiedByDateSame()
        {
            request.Headers.IfModifiedSince = package.LastUpdated;

            var result = DownloadPackage(HttpMethod.Get, "SomePackage", SampleVersion.SemanticVersion);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(await GetContent(result), Is.Empty);
        }

        [Test]
        public async Task GetPackageNotModifiedByDateOlder()
        {
            request.Headers.IfModifiedSince = package.LastUpdated.AddSeconds(1);

            var result = DownloadPackage(HttpMethod.Get, "SomePackage", SampleVersion.SemanticVersion);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(await GetContent(result), Is.Empty);
        }

        [Test]
        public async Task DownloadPackageSendsContent()
        {
            var result = DownloadPackage(HttpMethod.Get, "SomePackage", SampleVersion.SemanticVersion);

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await GetContent(result), Is.EqualTo("<fake package contents>"));
        }
        
        [Test]
        public async Task PutPackage()
        {
            luceneRepository.Setup(r => r.AddPackageAsync(package)).Returns(completeTask);

            var result = await controller.PutPackage(package);

            luceneRepository.Verify(r => r.AddPackageAsync(package));

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(result.Headers.Location, Is.EqualTo(new Uri("http://localhost/api/packages/Sample/1.0")));
        }

        [Test]
        public async Task PutPackageCannotBeNull()
        {
            var result = await controller.PutPackage(null);
            
            luceneRepository.Verify(r => r.AddPackageAsync(It.IsAny<IPackage>()), Times.Never());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task DeletePackageSpecCannotBeNull()
        {
            var result = await controller.DeletePackage(null);

            luceneRepository.Verify(r => r.RemovePackageAsync(It.IsAny<IPackage>()), Times.Never());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task DeletePackageRequiresId()
        {
            var result = await controller.DeletePackage("", "1.0");

            luceneRepository.Verify(r => r.RemovePackageAsync(It.IsAny<IPackage>()), Times.Never());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task DeletePackageNotFound()
        {
            luceneRepository.Setup(r => r.FindPackage(package.Id, package.Version.SemanticVersion)).Returns((IPackage)null);

            var result = await controller.DeletePackage(package.Id, package.Version.ToString());

            luceneRepository.Verify(r => r.AddPackageAsync(It.IsAny<IPackage>()), Times.Never());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task DeletePackage()
        {
            luceneRepository.Setup(r => r.FindPackage(package.Id, package.Version.SemanticVersion)).Returns(package);
            luceneRepository.Setup(r => r.RemovePackageAsync(package)).Returns(Task.FromResult(""));

            var result = await controller.DeletePackage(package.Id, package.Version.ToString());
            
            luceneRepository.VerifyAll();

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        [TestCase("DownloadPackage", typeof(HttpGetAttribute), typeof(HttpHeadAttribute))]
        public void NonStandardActionsAllowMethods(string action, params Type[] verbs)
        {
            AssertAuthenticationAttributePresent(action, verbs);
        }

        [Test]
        public void GetPackageInfo()
        {
            var v1 = CreatePackage(new StrictSemanticVersion("1.0"));

            packages.Add(v1);
            packages.Add(CreatePackage(new StrictSemanticVersion("2.0")));

            luceneRepository.Setup(r => r.LucenePackages).Returns(packages.AsQueryable());

            var result = (PackageWithVersionHistory)controller.GetPackageInfo(v1.Id, "1.0");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(package.Id));
            Assert.That(result.VersionHistory.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetPackageInfoPicksLatestVersion()
        {
            var v2 = CreatePackage(new StrictSemanticVersion("2.0"));

            packages.Add(v2);
            packages.Add(CreatePackage(new StrictSemanticVersion("1.0")));

            luceneRepository.Setup(r => r.LucenePackages).Returns(packages.AsQueryable());

            var result = (PackageWithVersionHistory)controller.GetPackageInfo(v2.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(package.Id));
            Assert.That(result.Version, Is.EqualTo(v2.Version));
        }

        [Test]
        public void GetPackageInfoNotFound()
        {
            luceneRepository.Setup(r => r.LucenePackages).Returns(packages.AsQueryable());

            var result = controller.GetPackageInfo("NoneSuch");

            Assert.That(result, Is.InstanceOf<HttpResponseMessage>());
            Assert.That(((HttpResponseMessage)result).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
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
                };
        }

        private HttpResponseMessage DownloadPackage(HttpMethod method, string packageId, SemanticVersion version)
        {
            request.Method = method;

            if (version != null)
            {
                mirroringRepository.Setup(r => r.FindPackage(packageId, version)).Returns(package);
            }
            else
            {
                luceneRepository.Setup(r => r.FindPackagesById(packageId)).Returns(packages);
            }

            luceneRepository.Setup(r => r.Convert(It.IsAny<IPackage>())).Returns<IPackage>(p => (LucenePackage)p);

            return controller.DownloadPackage(packageId, version != null ? version.ToString() : null);
        }

        private static async Task<string> GetContent(HttpResponseMessage result)
        {
            if (result.Content == null) return string.Empty;

            var stream = new MemoryStream();
            await result.Content.CopyToAsync(stream);
            return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
        }
    }
}