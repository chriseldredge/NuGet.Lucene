using System;
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
    public class LocalRequestAuthenticationModule : HttpModule
    {
        public UserStore Store { get; set; }

        protected override void Init()
        {
            Application.AuthenticateRequest += AuthenticateRequest;
        }

        private void AuthenticateRequest(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated || !Request.IsLocal) return;

            var identity = new GenericIdentity(Store.LocalAdministratorUsername, typeof(LocalRequestAuthenticationModule).Name);
            Context.User = new ApiUserPrincipal(identity, RoleNames.All.ToArray());
        }
    }
}