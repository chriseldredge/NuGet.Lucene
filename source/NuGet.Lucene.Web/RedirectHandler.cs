using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web
{
    /// <summary>
    /// Sends a 307 Redirect to the given route with specified route values.
    /// </summary>
    public class RedirectHandler : HttpMessageHandler
    {
        protected readonly string routeName;
        protected readonly object routeValues;

        /// <summary>
        /// When true, append a trailing slash to the resolved URL if one
        /// is not already present.
        /// </summary>
        public bool AppendTrailingSlash { get; set; }

        public RedirectHandler(string routeName)
            : this(routeName, new { })
        {
        }

        public RedirectHandler(string routeName, object routeValues)
        {
            this.routeName = routeName;
            this.routeValues = routeValues;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.CreateResponse(HttpStatusCode.TemporaryRedirect);
            response.Headers.Location = GetRedirectUri(request);
            return Task.FromResult(response);
        }

        protected virtual Uri GetRedirectUri(HttpRequestMessage request)
        {
            var url = new UrlHelper(request).Link(routeName, routeValues);
            if (AppendTrailingSlash && !url.EndsWith("/"))
            {
                url = url + "/";
            }
            return new Uri(url);
        }
    }
}