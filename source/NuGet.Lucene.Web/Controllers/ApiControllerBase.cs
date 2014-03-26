using System;
using System.Web.Http;
using Common.Logging;

namespace NuGet.Lucene.Web.Controllers
{
    public class ApiControllerBase : ApiController
    {
        private static readonly ILog AuditLog = LogManager.GetLogger("NuGet.Lucene.Audit");

        private string CurrentUserName
        {
            get { return User.Identity.IsAuthenticated ? User.Identity.Name : "anonymous client"; }
        }

        protected void Audit(string format, params object[] args)
        {
            AuditLog.Info(m => m("{0} requested by {1}", string.Format(format, args), CurrentUserName));
        }
    }
}