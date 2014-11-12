using System;
using Moq;
using NuGet.Lucene.Web.Formatters;
using NUnit.Framework;
using NuGet.Lucene.IO;
using System.IO;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Net.Http;
using NuGet.Lucene.Tests;
using System.Threading.Tasks;

namespace NuGet.Lucene.Web.Tests.Formatters
{
    [TestFixture]
    public class PackageFormDataMediaFormatterTests
    {
        Mock<ILucenePackageRepository> repositoryMock;
        ILucenePackageRepository repository;
        PackageFormDataMediaFormatter formatter;

        [SetUp]
        public void SetUp()
        {
            repositoryMock = new Mock<ILucenePackageRepository>();
            repository = repositoryMock.Object;
            formatter = new PackageFormDataMediaFormatter(repository);
        }

        [Test]
        [TestCase(typeof(IPackage))]
        [TestCase(typeof(ZipPackage))]
        public void SupportsTypes(Type type)
        {
            formatter.CanReadType(type);
        }

        [Test]
        public void StreamsContentToRepository()
        {
            var stream = new HashingWriteStream("**no-file**", new MemoryStream(), HashAlgorithm.Create("SHA256"));
            repositoryMock.Setup(r => r.CreateStreamForStagingPackage()).Returns(stream);

            var provider = formatter.CreateStreamProvider();
            var content = new StringContent("content");
            content.Headers.Add("Content-Disposition", @"form-data; name=""package""; filename=""package""");

            var result = provider.GetStream(content, content.Headers);

            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public async Task LoadsPackageFromStream()
        {
            const string ContentDisposition = "example";

            var stream = new HashingWriteStream("**no-file**", new MemoryStream(), HashAlgorithm.Create("SHA256"));
            var package = new TestPackage();
            repositoryMock.Setup(r => r.LoadStagedPackage(stream)).Returns(package);

            var provider = formatter.CreateStreamProvider();
            provider.AddStream(ContentDisposition, stream);

            var result = await formatter.ReadFormDataFromStreamAsync(provider);

            Assert.That(result, Is.SameAs(package));
        }

        [Test]
        public async Task DiscardsStreamOnFailure()
        {
            const string ContentDisposition = "example";

            var stream = new HashingWriteStream("**no-file**", new MemoryStream(), HashAlgorithm.Create("SHA256"));
            var exception = new Exception("invalid package");
            repositoryMock.Setup(r => r.LoadStagedPackage(stream)).Throws(exception);

            var provider = formatter.CreateStreamProvider();
            provider.AddStream(ContentDisposition, stream);

            try
            {
                await formatter.ReadFormDataFromStreamAsync(provider);
                Assert.Fail("Expected mock exception to be thrown.");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.SameAs(exception));
            }

            repositoryMock.Verify(r => r.DiscardStagedPackage(stream), Times.Once);
        }
    }
}
