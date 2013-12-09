using System;
using System.Security.Principal;

namespace NuGet.Lucene.Web.Modules
{
    public class LocalRequstAuthenticationModule : HttpModule
    {
        protected override void Init()
        {
            Application.AuthenticateRequest += AuthenticateRequest;
        }

        private void AuthenticateRequest(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated || !Request.IsLocal) return;

            var identity = new GenericIdentity("LocalAdministrator", typeof(LocalRequstAuthenticationModule).Name);
            var roles = new[] {RoleNames.AccountAdministrator, RoleNames.PackageManager};
            Context.User = new GenericPrincipal(identity, roles);
        }
    }
}