using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace NuGet.Lucene.Web.Filters
{
    public abstract class DefaultAcceptHeaderFilter : ActionFilterAttribute
    {
        private readonly IEnumerable<MediaTypeWithQualityHeaderValue> defaultMediaTypes;

        protected DefaultAcceptHeaderFilter(params MediaTypeWithQualityHeaderValue[] defaultMediaTypes)
        {
            this.defaultMediaTypes = defaultMediaTypes;
        }

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

            request.Headers.Accept.AddRange(defaultMediaTypes);
        }
    }

    public class DefaultAcceptAtomFilter : DefaultAcceptHeaderFilter
    {
        public DefaultAcceptAtomFilter() :
            base(new MediaTypeWithQualityHeaderValue("application/atom+xml"), new MediaTypeWithQualityHeaderValue("application/xml"))
        {
        }
    }

    public class DefaultAcceptJsonFilter : DefaultAcceptHeaderFilter
    {
        public DefaultAcceptJsonFilter() :
            base(new MediaTypeWithQualityHeaderValue("application/json"))
        {
        }
    }
}
