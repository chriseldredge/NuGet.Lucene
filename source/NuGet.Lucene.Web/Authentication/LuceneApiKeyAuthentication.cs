using System.Linq;
using System.Security.Principal;
using System.Web;

namespace NuGet.Lucene.Web.Authentication
{
    public class LuceneApiKeyAuthentication : IApiKeyAuthentication
    {
        public const string ApiKeyHeader = "X-NuGet-ApiKey";

        public IQueryable<ApiUser> Users { get; set; }

        public IPrincipal AuthenticateRequest(HttpRequestBase request)
        {
            var clientKey = request.Headers[ApiKeyHeader];

            if (!AuthenticationRequired || string.IsNullOrWhiteSpace(clientKey)) return null;

            var user = Users.FirstOrDefault(u => u.Key == clientKey);

            return user != null
                ? new GenericPrincipal(new GenericIdentity(user.Username, "NuGet Api Key Authentication"), new[] { "ApiUser" })
                : null;
        }
        
        public bool AuthenticationRequired
        {
            get { return NuGetWebApiModule.GetFlagFromAppSetting("requireApiKey", true); }
        }
    }
}