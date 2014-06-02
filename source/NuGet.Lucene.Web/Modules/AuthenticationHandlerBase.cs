using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace NuGet.Lucene.Web.Modules
{
    public abstract class AuthenticationHandlerBase : AuthenticationHandler<DefaultAuthenticationOptions>
    {
        protected Task<AuthenticationTicket> EmptyTicket()
        {
            return Task.FromResult<AuthenticationTicket>(null);
        }

        protected bool IsAuthenticated
        {
            get { return Request.User != null && Request.User.Identity.IsAuthenticated; }
        }

        protected string CurrentUsername
        {
            get { return IsAuthenticated ? Request.User.Identity.Name : null; }
        }
    }
}