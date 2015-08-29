using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace NuGet.Lucene.Web.Filters
{
    public class DefaultAcceptHeaderFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            SetDefaultAcceptHeader(actionContext.Request);
            base.OnActionExecuting(actionContext);
        }

        public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            SetDefaultAcceptHeader(actionContext.Request);
            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }

        private void SetDefaultAcceptHeader(HttpRequestMessage request)
        {
            if (request.Headers.Accept.Any()) return;

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/atom+xml"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        }
    }
}
