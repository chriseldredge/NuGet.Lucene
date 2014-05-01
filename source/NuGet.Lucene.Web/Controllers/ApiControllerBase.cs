using System;
using System.Web.Http;
using Common.Logging;

namespace NuGet.Lucene.Web.Controllers
{
    public class ApiControllerBase : ApiController
    {
        private static readonly ILog AuditLog = LogManager.GetLogger("NuGet.Lucene.Audit");

        protected string CurrentUserName
        {
            get { return IsUserAuthenticated ? User.Identity.Name : null; }
        }

        protected bool IsUserAuthenticated
        {
            get { return User != null && User.Identity != null && User.Identity.IsAuthenticated; }
        }

        protected void Audit(string format, params object[] args)
        {
            AuditLog.Info(m => m("{0} requested by {1}", string.Format(format, args), CurrentUserName ?? "anonymous client"));
        }
    }
}