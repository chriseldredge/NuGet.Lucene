using System.Security.Claims;
using Microsoft.Owin;

namespace NuGet.Lucene.Web.Authentication
{
    public interface IApiKeyAuthentication
    {
        ClaimsIdentity AuthenticateRequest(IOwinRequest request);
    }
}