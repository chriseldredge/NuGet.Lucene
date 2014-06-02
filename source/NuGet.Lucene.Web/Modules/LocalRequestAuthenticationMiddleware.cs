using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Modules
{
    /// <summary>
    /// When request originates from a local network address,
    /// authenticate the user as <c>"LocalAdministrator"</c>
    /// and grant all roles in <see cref="RoleNames.All"/>.
    /// </summary>
    public class LocalRequestAuthenticationMiddleware : AuthenticationMiddleware<DefaultAuthenticationOptions>
    {
        public UserStore Store { get; set; }

        public LocalRequestAuthenticationMiddleware(OwinMiddleware next)
            : base(next, new DefaultAuthenticationOptions(typeof(LocalRequestAuthenticationMiddleware).Name))
        {
        }

        protected override AuthenticationHandler<DefaultAuthenticationOptions> CreateHandler()
        {
            return new LocalRequestAuthenticationHandler(Store);
        }

        class LocalRequestAuthenticationHandler : UserStoreAuthenticationHandler
        {
            public LocalRequestAuthenticationHandler(UserStore store)
                : base(store)
            {
            }

            protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
            {
                if (IsAuthenticated || !Request.IsLocal())
                {
                    return EmptyTicket();
                }

                var identity = new GenericIdentity(store.LocalAdministratorUsername, typeof(LocalRequestAuthenticationMiddleware).Name);
                var claims = RoleNames.All.Select(r => new Claim(identity.RoleClaimType, r));
                var id = new ClaimsIdentity(identity, claims);
                var ticket = new AuthenticationTicket(id, new AuthenticationProperties());
                return Task.FromResult(ticket);
            }

        }
    }
}