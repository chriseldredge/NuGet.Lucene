using System.Security.Principal;

namespace NuGet.Lucene.Web.Authentication
{
    public class ApiUserPrincipal : GenericPrincipal
    {
        public ApiUserPrincipal(IIdentity identity, string[] roles) : base(identity, roles)
        {
        }
    }
}