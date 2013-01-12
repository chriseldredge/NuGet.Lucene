using System.Configuration;
using System.Web;

namespace NuGet.Lucene.Web
{
    public class ApiKeyAuthentication
    {
        public const string ApiKeyHeader = "X-NuGet-ApiKey";

        public bool AuthenticationRequired
        {
            get { return ApplicationConfig.GetFlagFromAppSetting("requireApiKey", true); }
        }

        public bool AuthenticateRequest(HttpRequestBase request)
        {
            if (!AuthenticationRequired) return true;

            return Equals(request.Headers[ApiKeyHeader], ConfigurationManager.AppSettings["apiKey"]);
        }
    }
}