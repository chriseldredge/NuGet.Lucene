using System;
using System.Net;
using System.Security.Authentication;
using System.Web;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Modules
{
    public class ApiKeyAuthenticationModule : HttpModule
    {
        public IApiKeyAuthentication Service { get; set; }

        protected override void Init()
        {
            Application.AuthenticateRequest += AuthenticateRequest;
        }

        private void AuthenticateRequest(object sender, EventArgs e)
        {
            try
            {
                var apiUser = Service.AuthenticateRequest(Request);
                if (apiUser != null)
                {
                    Context.User = apiUser;
                }
            }
            catch (AuthenticationException ex)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                Response.StatusDescription = ex.Message;
                Response.End();
            }
        }
    }
}