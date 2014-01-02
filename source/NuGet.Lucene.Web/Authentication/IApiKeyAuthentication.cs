using System.Security.Principal;
using System.Web;

namespace NuGet.Lucene.Web.Authentication
{
    public interface IApiKeyAuthentication
    {
        IPrincipal AuthenticateRequest(HttpRequestBase request);
    }
}