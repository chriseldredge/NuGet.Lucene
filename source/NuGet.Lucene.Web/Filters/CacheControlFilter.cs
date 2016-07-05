using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace NuGet.Lucene.Web.Filters
{
    public class CacheControlFilter : ActionFilterAttribute
    {
        public override Task OnActionExecutedAsync(HttpActionExecutedContext context, CancellationToken cancellationToken)
        {
            context.Response.Headers.CacheControl = new CacheControlHeaderValue
            {
                MustRevalidate = true,
                Private = true,
                MaxAge = TimeSpan.Zero
            };
            return base.OnActionExecutedAsync(context, cancellationToken);
        }
    }
}
