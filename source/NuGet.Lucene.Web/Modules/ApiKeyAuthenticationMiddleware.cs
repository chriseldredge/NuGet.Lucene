using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Modules
{
    public class ApiKeyAuthenticationMiddleware : AuthenticationMiddleware<DefaultAuthenticationOptions>
    {
        public IApiKeyAuthentication Service { get; set; }

        public ApiKeyAuthenticationMiddleware(OwinMiddleware next) : base(next, new DefaultAuthenticationOptions(typeof(ApiKeyAuthenticationMiddleware).Name))
        {
        }

        protected override AuthenticationHandler<DefaultAuthenticationOptions> CreateHandler()
        {
            return new ApiKeyAuthenticationHandler(Service);
        }

        private class ApiKeyAuthenticationHandler : AuthenticationHandler<DefaultAuthenticationOptions>
        {
            private readonly IApiKeyAuthentication service;
            private bool authenticationErrored;

            public ApiKeyAuthenticationHandler(IApiKeyAuthentication service)
            {
                this.service = service;
            }

            protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
            {
                try
                {
                    var identity = service.AuthenticateRequest(Request);
                    if (identity != null)
                    {
                        return Task.FromResult(new AuthenticationTicket(identity, new AuthenticationProperties()));
                    }
                }
                catch (AuthenticationException)
                {
                    Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    Response.ReasonPhrase = "Invalid API key";
                    authenticationErrored = true;
                }

                return Task.FromResult<AuthenticationTicket>(null);
            }

            public override Task<bool> InvokeAsync()
            {
                return Task.FromResult(authenticationErrored);
            }
        }
    }
}