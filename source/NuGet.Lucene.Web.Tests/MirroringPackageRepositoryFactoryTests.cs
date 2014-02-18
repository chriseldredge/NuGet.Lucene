using System;
using System.Net;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    [TestFixture]
    public class MirroringPackageRepositoryFactoryTests
    {
        [Test]
        public void CreateWithoutOrigin()
        {
            var result = MirroringPackageRepositoryFactory.Create(new LocalPackageRepository("."), "", TimeSpan.Zero, false);

            Assert.That(result.MirroringEnabled, Is.False, "MirroringEnabled");
        }

        [Test]
        public void CreateWithOrigin()
        {
            var result = MirroringPackageRepositoryFactory.Create(new LocalPackageRepository("."),
                                                                  "http://example.com/packages/", TimeSpan.Zero, false);

            Assert.That(result.MirroringEnabled, Is.True, "MirroringEnabled");
        }

        [Test]
        public void CreateWithOriginSetAsLocal()
        {
            var result = MirroringPackageRepositoryFactory.Create(new LocalPackageRepository("."),
                                                                  "http://localhost/packages/", TimeSpan.Zero, false);

            Assert.That(result.AlwaysCheckMirrorOveride, Is.True, "IsLocalMirror");
        }

        [Test]
        public void CreateWithAlwaysCheckMirrorSet()
        {
            var result = MirroringPackageRepositoryFactory.Create(new LocalPackageRepository("."),
                                                                  "http://example.com/packages/", TimeSpan.Zero, true);

            Assert.That(result.AlwaysCheckMirrorOveride, Is.True, "IsLocalMirror");
        }

        [Test]
        public void OriginRepositorySendsCustomUserAgent()
        {
            var client = new Mock<IHttpClient>();
            var request = (HttpWebRequest) WebRequest.Create("http://example.com/");

            MirroringPackageRepositoryFactory.CreateDataServicePackageRepository(client.Object, TimeSpan.FromMilliseconds(1234));
            client.Raise(c => c.SendingRequest += null, new WebRequestEventArgs(request));

            Assert.That(request.UserAgent, Is.StringContaining("NuGet.Lucene.Web"));
            Assert.That(request.Headers[RepositoryOperationNames.OperationHeaderName], Is.EqualTo(RepositoryOperationNames.Mirror));
            Assert.That(request.Timeout, Is.EqualTo(1234));
        }
    }
}