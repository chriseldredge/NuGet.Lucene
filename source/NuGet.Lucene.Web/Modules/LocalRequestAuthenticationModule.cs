using System;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace NuGet.Lucene.Web.Modules
{
    /// <summary>
    /// When <see cref="HttpRequestBase.IsLocal"/> is true,
    /// and the request has not already been authenticated, this module
    /// will authenticate the user as <c>"LocalAdministrator"</c>
    /// and grant all roles in <see cref="RoleNames.All"/>.
    /// </summary>
    public class LocalRequestAuthenticationModule : HttpModule
    {
        protected override void Init()
        {
            Application.AuthenticateRequest += AuthenticateRequest;
        }

        private void AuthenticateRequest(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated || !Request.IsLocal) return;

            var identity = new GenericIdentity("LocalAdministrator", typeof(LocalRequestAuthenticationModule).Name);
            Context.User = new GenericPrincipal(identity, RoleNames.All.ToArray());
        }
    }
}