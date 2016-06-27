using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace NuGet.Lucene.Web.Filters
{
    /// <summary>
    /// The Katana project (OWIN) incorrectly escapes/encodes characters such as parentheses
    /// on incoming request paths which in turn causes `next link` in paginated responses to
    /// be malformed. This filter unencodes these characters to work around the issue.
    /// </summary>
    public class OwinPathEncodingFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            FixIncorrectPathEncoding(actionContext);
            base.OnActionExecuting(actionContext);
        }

        private void FixIncorrectPathEncoding(HttpActionContext actionContext)
        {
            var requestUri = actionContext.Request.RequestUri;

            if (!requestUri.AbsolutePath.Contains("%")) return;

            var uriBuilder = new UriBuilder(requestUri);

            uriBuilder.Path = uriBuilder.Path.Replace("%28", "(").Replace("%29", ")").Replace("%3D", "=").Replace("%2C", ",");
            actionContext.Request.RequestUri = uriBuilder.Uri;
        }
    }
}
