using System.Linq;
using System.Net.Http;
using System.Web.Http.Hosting;
using NUnit.Framework;
using NuGet.Lucene.Web.MessageHandlers;

namespace NuGet.Lucene.Web.Tests
{
    public abstract class CrossOriginMessageHandlerTests : RouteTests
    {
        [TestFixture]
        public class PackagesController : CrossOriginMessageHandlerTests
        {
            [Test]
            public void PackageDownload()
            {
                var methods = GetSupportedMethods("api/packages/Sample/1.0/content");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get, HttpMethod.Head }));
            }

            [Test]
            public void PackageDownloadAnyVersion()
            {
                var methods = GetSupportedMethods("api/packages/Sample/content");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get, HttpMethod.Head }));
            }

            [Test]
            public void Search()
            {
                var methods = GetSupportedMethods("api/packages");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get, HttpMethod.Put }));
            }

            [Test]
            public void InvalidVersion()
            {
                var methods = GetSupportedMethods("api/packages/SamplePackage/notaversion");

                Assert.That(methods, Is.Empty);
            }

            [Test]
            public void GetOrDeleteWithValidVersion()
            {
                var methods = GetSupportedMethods("api/packages/SamplePackage/1.0");

                Assert.That(methods, Is.EquivalentTo(new[] {HttpMethod.Get, HttpMethod.Delete}));
            }
        }

        [TestFixture]
        public class IndexingController : CrossOriginMessageHandlerTests
        {
            [Test]
            public void Status()
            {
                var methods = GetSupportedMethods("api/indexing/status");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get }));
            }

            [Test]
            public void Synchronize()
            {
                var methods = GetSupportedMethods("api/indexing/synchronize");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Post }));
            }

            [Test]
            public void Cancel()
            {
                var methods = GetSupportedMethods("api/indexing/cancel");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Post }));
            }
        }

        [TestFixture]
        public class UsersController : CrossOriginMessageHandlerTests
        {
            [Test]
            public void GetAll()
            {
                var methods = GetSupportedMethods("api/users");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get, HttpMethod.Delete }));
            }

            [Test]
            public void SpecificUser()
            {
                var methods = GetSupportedMethods("api/users/example");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete }));
            }

            [Test]
            public void SpecificUserWithSlash()
            {
                var methods = GetSupportedMethods("api/users/domain/user");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete }));
            }
        }

        [TestFixture]
        public class TabCompletionController : CrossOriginMessageHandlerTests
        {
            [Test]
            public void PackageIds()
            {
                var methods = GetSupportedMethods("api/v2/package-ids");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get }));
            }

            [Test]
            public void PackageVersions()
            {
                var methods = GetSupportedMethods("api/v2/package-versions/SamplePackage");

                Assert.That(methods, Is.EquivalentTo(new[] { HttpMethod.Get }));
            }
        }

        private HttpMethod[] GetSupportedMethods(string appRelativeUri)
        {
            return CrossOriginMessageHandler.GetMatchingApis(routes, CreateOptionsRequest(appRelativeUri))
                .Select(i => new HttpMethod(i.Method))
                .ToArray();
        }

        private HttpRequestMessage CreateOptionsRequest(string appRelativeUri)
        {
            var absoluteUri = "http://localhost/" + appRelativeUri;
            var request = new HttpRequestMessage(HttpMethod.Options, absoluteUri);
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, configuration);
            return request;
        }
    }
}