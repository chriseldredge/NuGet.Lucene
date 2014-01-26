using System;
using System.Security.Principal;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Modules
{
    /// <summary>
    /// When the request has not already been authenticated, this
    /// module will authenticate the user and grant the
    /// <see cref="RoleNames.PackageManager"/> role.
    /// </summary>
    public class AnonymousPackageManagerModule : HttpModule
    {
        protected override void Init()
        {
            Application.AuthenticateRequest += AuthenticateRequest;
        }

        private void AuthenticateRequest(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated) return;

            var identity = new GenericIdentity("AnonymousPackageManager", typeof(AnonymousPackageManagerModule).Name);
            var roles = new[] {RoleNames.PackageManager};
            Context.User = new ApiUserPrincipal(identity, roles);
        }
    }
}