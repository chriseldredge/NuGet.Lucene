using System.Threading.Tasks;
using NuGet.Lucene.Web.Middleware;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Middleware
{
    [TestFixture]
    public class AnonymousPackageManagerMiddlewareTests : AuthenticationMiddlewareTestBase
    {
        private AnonymousPackageManagerMiddleware middleware;

        [SetUp]
        public void SetUp()
        {
            middleware = new AnonymousPackageManagerMiddleware(next);
        }

        [Test]
        public async Task NotAuthenticated_GrantsRole()
        {
            await middleware.Invoke(context);

            Assert.That(request.User.Identity.IsAuthenticated, Is.True, "IsAuthenticated");
            Assert.That(request.User.IsInRole(RoleNames.PackageManager), Is.True, "IsInRole(RoleNames.PackageManager)");
        }

        [Test]
        public async Task Authenticated_DoesNotGrantsRole()
        {
            request.User = user;

            await middleware.Invoke(context);

            Assert.That(request.User.IsInRole(RoleNames.PackageManager), Is.False, "IsInRole(RoleNames.PackageManager)");
        }
    }
}