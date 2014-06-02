using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Modules
{
    public class RoleMappingAuthenticationMiddleware : AuthenticationMiddleware<DefaultAuthenticationOptions>
    {
        public UserStore Store { get; set; }

        public RoleMappingAuthenticationMiddleware(OwinMiddleware next)
            : base(next, new DefaultAuthenticationOptions(typeof(RoleMappingAuthenticationMiddleware).Name))
        {
        }

        protected override AuthenticationHandler<DefaultAuthenticationOptions> CreateHandler()
        {
            return new RoleMappingAuthenticationHandler(Store);
        }

        private class RoleMappingAuthenticationHandler : UserStoreAuthenticationHandler
        {
            private static string[] Empty = new String[0];

            public RoleMappingAuthenticationHandler(UserStore store) : base(store)
            {
            }

            protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
            {
                if (!IsAuthenticated)
                {
                    return EmptyTicket();
                }

                var apiUser = store.FindByUsername(CurrentUsername);
                var isNew = false;

                if (apiUser == null)
                {
                    isNew = true;
                    apiUser = new ApiUser { Username = CurrentUsername };
                }

                var origRoles = (apiUser.Roles ?? Empty).ToArray();
                var missingRoles = RoleNames.All.Except(origRoles);
                var implicitGrants = GetUserRoles(Request.User, missingRoles).ToArray();
                apiUser.Roles = origRoles.Union(implicitGrants).Distinct().ToArray();

                if (isNew || !apiUser.Roles.SequenceEqual(origRoles))
                {
                    store.Add(apiUser, UserUpdateMode.Overwrite);
                }

                if (implicitGrants.Any())
                {
                    var identity = new ClaimsIdentity(
                        new GenericIdentity(CurrentUsername, typeof(RoleMappingAuthenticationMiddleware).Name),
                        implicitGrants.Select(g => new Claim(ClaimsIdentity.DefaultRoleClaimType, g)));
                    return Task.FromResult(new AuthenticationTicket(identity, new AuthenticationProperties()));
                }

                return EmptyTicket();
            }

            private IEnumerable<string> GetUserRoles(IPrincipal user, IEnumerable<string> missingRoles)
            {
                var roleMappings = NuGetWebApiModule.RoleMappings;
                return missingRoles.Where(role =>
                {
                    var aliases = (roleMappings.Get(role) ?? "")
                        .Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim());

                    var rolesToTest = new[] {role}.Union(aliases).Distinct();

                    return rolesToTest.Any(user.IsInRole);
                });
            }
        }
    }
}