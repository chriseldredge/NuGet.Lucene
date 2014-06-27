using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGet.Lucene.Web.Middleware
{
    /// <summary>
    /// When the request has not already been authenticated, this
    /// module will authenticate the user and grant the
    /// <see cref="RoleNames.PackageManager"/> role.
    /// </summary>
    public class AnonymousPackageManagerMiddleware : AuthenticationMiddlewareBase
    {
        public AnonymousPackageManagerMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        protected override AuthenticationHandlerBase CreateHandler()
        {
            return new AnonymousPackageManagerAuthenticationHandler();
        }

        private class AnonymousPackageManagerAuthenticationHandler : AuthenticationHandlerBase
        {
            protected override Task AuthenticateCoreAsync()
            {
                if (!IsAuthenticated)
                {
                    var identity = new GenericIdentity("AnonymousPackageManager", typeof(AnonymousPackageManagerMiddleware).Name);
                    Request.User = new GenericPrincipal(identity, new[] { RoleNames.PackageManager });
                }

                return completedTask;
            }
        }
    }
}