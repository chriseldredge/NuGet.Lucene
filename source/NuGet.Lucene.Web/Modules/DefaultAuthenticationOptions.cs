using Microsoft.Owin.Security;

namespace NuGet.Lucene.Web.Modules
{
    public class DefaultAuthenticationOptions : AuthenticationOptions
    {
        public DefaultAuthenticationOptions(string authenticationType) : base(authenticationType)
        {
        }
    }
}