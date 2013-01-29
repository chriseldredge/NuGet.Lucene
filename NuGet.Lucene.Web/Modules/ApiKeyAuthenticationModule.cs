using System;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Modules
{
    public class ApiKeyAuthenticationModule : HttpModule
    {
        public IApiKeyAuthentication service { get; set; }

        protected override void Init()
        {
            Application.AuthenticateRequest += AuthenticateRequest;
        }

        private void AuthenticateRequest(object sender, EventArgs e)
        {
            var apiUser = service.AuthenticateRequest(Request);
            if (apiUser != null)
            {
                Context.User = apiUser;
            }
        }
    }
}