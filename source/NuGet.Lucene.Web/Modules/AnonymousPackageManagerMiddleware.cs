using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace NuGet.Lucene.Web.Modules
{
    /// <summary>
    /// When the request has not already been authenticated, this
    /// module will authenticate the user and grant the
    /// <see cref="RoleNames.PackageManager"/> role.
    /// </summary>
    public class AnonymousPackageManagerMiddleware : AuthenticationMiddleware<DefaultAuthenticationOptions>
    {
        public AnonymousPackageManagerMiddleware(OwinMiddleware next)
            : base(next, new DefaultAuthenticationOptions(typeof(AnonymousPackageManagerMiddleware).Name))
        {
        }

        protected override AuthenticationHandler<DefaultAuthenticationOptions> CreateHandler()
        {
            return new AnonymousPackageManagerAuthenticationHandler();
        }

        private class AnonymousPackageManagerAuthenticationHandler : AuthenticationHandlerBase
        {
            protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
            {
                if (IsAuthenticated)
                {
                    return EmptyTicket();
                }

                var identity = new ClaimsIdentity(
                    new GenericIdentity("AnonymousPackageManager", typeof (AnonymousPackageManagerMiddleware).Name),
                    new[] { new Claim(ClaimsIdentity.DefaultRoleClaimType, RoleNames.PackageManager) });
                return Task.FromResult(new AuthenticationTicket(identity, new AuthenticationProperties()));
            }
        }
    }
}