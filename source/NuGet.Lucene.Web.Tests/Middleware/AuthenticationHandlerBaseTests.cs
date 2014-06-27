using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Lucene.Web.Authentication;
using NuGet.Lucene.Web.Middleware;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Middleware
{
    [TestFixture]
    public class AuthenticationHandlerBaseTests : AuthenticationMiddlewareTestBase
    {
        private TestableAuthenticationHandler handler;

        [SetUp]
        public void SetUp()
        {
            handler = new TestableAuthenticationHandler();
        }

        [Test]
        public async Task InitializeAsyncMakesContextAvailable()
        {
            await handler.InitializeAsync(context);

            handler.AssertContextAvailable(context);
        }

        [Test]
        public async Task IsAuthenticated_False()
        {
            await handler.InitializeAsync(context);

            Assert.That(handler.IsAuthenticated, Is.False, "IsAuthenticated");
        }

        [Test]
        public async Task IsAuthenticated_True()
        {
            await handler.InitializeAsync(context);

            request.User = new ApiUserPrincipal(new GenericIdentity("Velociraptor"), new string[0]);

            Assert.That(handler.IsAuthenticated, Is.True, "IsAuthenticated");
        }

        [Test]
        public async Task CurrentUsername()
        {
            await handler.InitializeAsync(context);

            request.User = new ApiUserPrincipal(new GenericIdentity("Velociraptor"), new string[0]);

            Assert.That(handler.CurrentUsername, Is.EqualTo("Velociraptor"), "CurrentUsername");
        }

        [Test]
        public async Task CurrentUsername_NullWhenNotAuthenticated()
        {
            await handler.InitializeAsync(context);

            Assert.That(handler.CurrentUsername, Is.Null, "CurrentUsername");
        }

        class TestableAuthenticationHandler : AuthenticationHandlerBase
        {
            public void AssertContextAvailable(IOwinContext owinContext)
            {
                Assert.That(Context, Is.SameAs(owinContext));
                Assert.That(Request, Is.SameAs(owinContext.Request));
                Assert.That(Response, Is.SameAs(owinContext.Response));
            }

            public new bool IsAuthenticated
            {
                get { return base.IsAuthenticated; }
            }

            public new string CurrentUsername
            {
                get { return base.CurrentUsername; }
            }
        }
    }
}