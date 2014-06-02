using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using Microsoft.Owin;

namespace NuGet.Lucene.Web.Authentication
{
    public class LuceneApiKeyAuthentication : IApiKeyAuthentication
    {
        public const string ApiKeyHeader = "X-NuGet-ApiKey";

        public UserStore Store { get; set; }

        public ClaimsIdentity AuthenticateRequest(IOwinRequest request)
        {
            var clientKey = request.Headers[ApiKeyHeader];

            if (string.IsNullOrWhiteSpace(clientKey)) return null;

            var user = Store.FindByKey(clientKey);

            if (user == null)
            {
                throw new AuthenticationException("Invalid API key.");
            }
            
            return new ClaimsIdentity(
                new GenericIdentity(user.Username, typeof(LuceneApiKeyAuthentication).Name),
                user.Roles.Select(r => new Claim(ClaimsIdentity.DefaultRoleClaimType, r)));
            
        }
    }
}