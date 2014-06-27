using System.Security.Authentication;
using System.Security.Principal;
using Microsoft.Owin;

namespace NuGet.Lucene.Web.Authentication
{
    public interface IApiKeyAuthentication
    {
        /// <summary>
        /// Authenticate a request using the <c>X-NuGet-ApiKey</c> request header.
        /// </summary>
        /// <throws><see cref="AuthenticationException"/> when the key is specified but not valid.</throws>
        IPrincipal AuthenticateRequest(IOwinRequest request);
    }
}