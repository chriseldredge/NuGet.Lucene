using System.Security.Principal;
using System.Web;

namespace NuGet.Lucene.Web.Authentication
{
    public interface IApiKeyAuthentication
    {
        bool AuthenticationRequired { get; }
        IPrincipal AuthenticateRequest(HttpRequestBase request);
    }
}