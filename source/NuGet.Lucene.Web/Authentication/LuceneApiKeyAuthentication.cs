using System.Linq;
using System.Security.Authentication;
using System.Security.Principal;
using System.Web;

namespace NuGet.Lucene.Web.Authentication
{
    public class LuceneApiKeyAuthentication : IApiKeyAuthentication
    {
        public const string ApiKeyHeader = "X-NuGet-ApiKey";

        public UserStore Store { get; set; }

        public IPrincipal AuthenticateRequest(HttpRequestBase request)
        {
            var clientKey = request.Headers[ApiKeyHeader];

            if (string.IsNullOrWhiteSpace(clientKey)) return null;

            var user = Store.FindByKey(clientKey);

            if (user == null)
            {
                throw new AuthenticationException("Invalid API key.");
            }

            return new GenericPrincipal(new GenericIdentity(user.Username, "NuGet Api Key Authentication"), user.Roles.ToArray());
        }
    }
}