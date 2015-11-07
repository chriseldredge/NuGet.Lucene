using System.Net;
using System.Security.Authentication;
using System.Security.Principal;
using System.Threading.Tasks;
using Moq;
using NuGet.Lucene.Web.Authentication;
using NuGet.Lucene.Web.Middleware;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Middleware
{
    [TestFixture]
    public class ApiKeyAuthenticationMiddlewareTests : AuthenticationMiddlewareTestBase
    {
        private ApiKeyAuthenticationMiddleware middleware;
        private Mock<IApiKeyAuthentication> serviceMock;

        [SetUp]
        public void SetUp()
        {
            middleware = new ApiKeyAuthenticationMiddleware(nextMock.Object);
            serviceMock = new Mock<IApiKeyAuthentication>();
            middleware.Service = serviceMock.Object;
        }

        [Test]
        public async Task AuthenticateReturnsNull_InvokesNext()
        {
            await middleware.Invoke(contextMock.Object);

            nextMock.Verify(m => m.Invoke(contextMock.Object), Times.Once);
        }

        [Test]
        public async Task AuthenticateReturnsNull_LeavesUserNull()
        {
            await middleware.Invoke(contextMock.Object);

            Assert.That(requestMock.Object.User, Is.Null, "Request.User");
        }

        [Test]
        public async Task AuthenticateReturnsUser_SetsRequestUser()
        {
            serviceMock.Setup(m => m.AuthenticateRequest(requestMock.Object)).Returns(user);

            await middleware.Invoke(contextMock.Object);

            Assert.That(requestMock.Object.User, Is.SameAs(user), "Request.User");
        }

        [Test]
        public async Task AuthenticateReturnsUser_DecoratesRequestUser()
        {
            const string upstreamRole = "Upstream Role";
            var upstreamUser = new ApiUserPrincipal(new GenericIdentity("UpstreamUser"), new [] {upstreamRole});
            requestMock.Object.User = upstreamUser;
            serviceMock.Setup(m => m.AuthenticateRequest(requestMock.Object)).Returns(user);

            await middleware.Invoke(contextMock.Object);

            Assert.That(requestMock.Object.User.Identity, Is.SameAs(upstreamUser.Identity), "Request.User.Identity");
            Assert.That(requestMock.Object.User.IsInRole(upstreamRole), Is.True, "Request.User.IsInRole(upstreamRole)");
            Assert.That(requestMock.Object.User.IsInRole(Role1), Is.True, "Request.User.IsInRole(Role1)");
        }

        [Test]
        public async Task AuthenticateReturnsUser_InvokesNext()
        {
            serviceMock.Setup(m => m.AuthenticateRequest(requestMock.Object)).Returns(user);

            await middleware.Invoke(contextMock.Object);

            nextMock.Verify(m => m.Invoke(contextMock.Object), Times.Once);
        }

        [Test]
        public async Task AuthenticateReturnsThrows_DoesNotInvokeNext()
        {
            serviceMock.Setup(m => m.AuthenticateRequest(requestMock.Object)).Throws<AuthenticationException>();

            await middleware.Invoke(contextMock.Object);

            nextMock.Verify(m => m.Invoke(contextMock.Object), Times.Never);
        }

        [Test]
        public async Task AuthenticateReturnsThrows_SetsResponseStatus()
        {
            serviceMock.Setup(m => m.AuthenticateRequest(requestMock.Object)).Throws<AuthenticationException>();

            await middleware.Invoke(contextMock.Object);

            Assert.That(responseMock.Object.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized), "Response.StatusCode");
        }

        [Test]
        public async Task AuthenticateReturnsThrows_SetsResponseReasonPhrase()
        {
            serviceMock.Setup(m => m.AuthenticateRequest(requestMock.Object)).Throws<AuthenticationException>();

            await middleware.Invoke(contextMock.Object);

            Assert.That(responseMock.Object.ReasonPhrase, Is.EqualTo(ApiKeyAuthenticationMiddleware.InvalidApiKeyReasonPhrase), "Response.ReasonPhrase");
        }
    }
}
