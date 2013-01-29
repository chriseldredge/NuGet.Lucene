using System.Net;
using System.Web;
using System.Web.Mvc;

namespace NuGet.Lucene.Web.Mvc
{
    public class HttpStatusCreatedResult : HttpStatusCodeResult
    {
        private readonly string location;

        public HttpStatusCreatedResult(string location) : base(HttpStatusCode.Created)
        {
            this.location = location;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var resolved = VirtualPathUtility.ToAbsolute(location);
            context.HttpContext.Response.RedirectLocation = resolved;
            base.ExecuteResult(context);
        }
    }
}