using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.MessageHandlers
{
    /// <summary>
    /// Adds support for OPTIONS requests and adds Cross-Origin Resource Sharing (CORS)
    /// response headers.
    /// </summary>
    public class CrossOriginMessageHandler : DelegatingHandler
    {
        private readonly bool enableCrossDomainRequests;

        public CrossOriginMessageHandler(bool enableCrossDomainRequests)
        {
            this.enableCrossDomainRequests = enableCrossDomainRequests;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Task<HttpResponseMessage> task;

            if (request.Method == HttpMethod.Options)
            {
                task = HandleOptionsRequestAsync(request);
            }
            else
            {
                task = base.SendAsync(request, cancellationToken);
            }

            var response = await task;

            if (enableCrossDomainRequests)
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Headers", "Accept, Origin, X-NuGet-ApiKey");
            }

            return response;
        }

        private async Task<HttpResponseMessage> HandleOptionsRequestAsync(HttpRequestMessage request)
        {
            var apis = GetMatchingApis(request.GetConfiguration().Routes, request).ToList();

            if (!apis.Any())
                return await Task.FromResult(request.CreateResponse(HttpStatusCode.NotFound));

            var supportedMethods = apis.Select(i => i.Method)
                                       .Distinct()
                                       .ToList();

            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Headers.Add("Access-Control-Allow-Methods", string.Join(",", supportedMethods));
            resp.Content = new ObjectContent(typeof(IEnumerable<SimpleApiDescription>), apis, request.GetConfiguration().Formatters.JsonFormatter);
            return await Task.FromResult(resp);
        }

        public static IEnumerable<SimpleApiDescription> GetMatchingApis(HttpRouteCollection routes, HttpRequestMessage request)
        {
            request = CopyRequest(request);

            var apiExplorer = request.GetConfiguration().Services.GetApiExplorer();

            foreach (var desc in apiExplorer.ApiDescriptions)
            {
                var route = desc.Route;
                object constraint;
                if (route.Constraints.TryGetValue("httpMethod", out constraint))
                {
                    var httpMethodConstraint = (HttpMethodConstraint) constraint;
                    if (!httpMethodConstraint.AllowedMethods.Contains(desc.HttpMethod))
                    {
                        continue;
                    }
                }
                request.Method = desc.HttpMethod;

                var routeData = route.GetRouteData("/", request);

                if (routeData == null)
                {
                    continue;
                }

                object requestedAction;

                if (!routeData.Values.TryGetValue("action", out requestedAction) ||
                    string.Equals(requestedAction as string, desc.ActionDescriptor.ActionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return new SimpleApiDescription(request, desc);
                }
            }
        }

        private static HttpRequestMessage CopyRequest(HttpRequestMessage request)
        {
            var copy = new HttpRequestMessage(request.Method, request.RequestUri);
            copy.Properties.AddRange(request.Properties);

            foreach (var h in request.Headers)
            {
                copy.Headers.Add(h.Key, h.Value);
            }

            return copy;
        }
    }
}