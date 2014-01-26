using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Modules
{
    /// <summary>
    /// When <see cref="HttpRequestBase.IsLocal"/> is true,
    /// and the request has not already been authenticated, this module
    /// will authenticate the user as <c>"LocalAdministrator"</c>
    /// and grant all roles in <see cref="RoleNames.All"/>.
    /// </summary>
    public class RoleMappingAuthenticationModule : HttpModule
    {
        private static string[] Empty = new String[0];

        public UserStore Store { get; set; }
        
        protected override void Init()
        {
            Application.PostAuthenticateRequest += ReplacePrincipalWithRoleMappings;
        }

        private void ReplacePrincipalWithRoleMappings(object sender, EventArgs e)
        {
            if (!Request.IsAuthenticated || Context.User is ApiUserPrincipal) return;

            var username = Context.User.Identity.Name;
            var apiUser = Store.FindByUsername(username);
            var isNew = false;

            if (apiUser == null)
            {
                isNew = true;
                apiUser = new ApiUser {Username = username};
            }

            var origRoles = (apiUser.Roles ?? Empty).ToArray();
            var missingRoles = RoleNames.All.Except(origRoles);
            apiUser.Roles = (origRoles).Union(GetUserRoles(Context.User, missingRoles)).Distinct().ToArray();

            if (isNew || !apiUser.Roles.SequenceEqual(origRoles))
            {
                Store.Add(apiUser, UserUpdateMode.Overwrite);
            }

            var identity = new GenericIdentity(apiUser.Username, typeof(RoleMappingAuthenticationModule).Name);
            Context.User = new GenericPrincipal(identity, apiUser.Roles.ToArray());
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