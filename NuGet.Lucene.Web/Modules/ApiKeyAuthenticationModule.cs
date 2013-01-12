using System;
using System.Security.Principal;

namespace NuGet.Lucene.Web.Modules
{
    public class ApiKeyAuthenticationModule : HttpModule
    {
        private ApiKeyAuthentication service;

        protected override void Init()
        {
            Application.AuthenticateRequest += AuthenticateRequest;
            service = new ApiKeyAuthentication();
        }

        private void AuthenticateRequest(object sender, EventArgs e)
        {
            if (service.AuthenticateRequest(Request))
            {
                Context.User = new GenericPrincipal(new GenericIdentity("ApiUser", "NuGet Api Key Authentication"), new[] { "ApiUser" });
            }
        }
    }
}