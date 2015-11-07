using System.Net;
using System.Security.Authentication;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Middleware
{
    public class ApiKeyAuthenticationMiddleware : AuthenticationMiddlewareBase
    {
        public const string InvalidApiKeyReasonPhrase = "Invalid API key";
        public IApiKeyAuthentication Service { get; set; }

        public ApiKeyAuthenticationMiddleware(OwinMiddleware next) : base(next)
        {
        }

        protected override AuthenticationHandlerBase CreateHandler()
        {
            return new ApiKeyAuthenticationHandler(Service);
        }

        private class ApiKeyAuthenticationHandler : AuthenticationHandlerBase
        {
            private readonly IApiKeyAuthentication service;

            public ApiKeyAuthenticationHandler(IApiKeyAuthentication service)
            {
                this.service = service;
            }

            protected override Task<bool> AuthenticateAsync()
            {
                try
                {
                    var identity = service.AuthenticateRequest(Request);
                    if (identity != null)
                    {
                        SetRequestUser(identity);
                    }
                    return Task.FromResult(true);
                }
                catch (AuthenticationException)
                {
                    Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    Response.ReasonPhrase = InvalidApiKeyReasonPhrase;
                    return Task.FromResult(false);
                }
            }

            protected virtual void SetRequestUser(IPrincipal identity)
            {
                if (IsAuthenticated)
                {
                    identity = new  CompositeRolePrincipalWrapper(Request.User, identity);
                }

                Request.User = identity;
            }
        }
    }
}