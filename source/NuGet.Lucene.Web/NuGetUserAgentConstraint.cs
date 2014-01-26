using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web
{
    /// <summary>
    /// Constrains a route mapping to only match when the UserAgent product contains
    /// <c>"NuGet"</c> (ignoring case).
    /// </summary>
    public class NuGetUserAgentConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values,
            HttpRouteDirection routeDirection)
        {
            return request.Headers.UserAgent
                .Where(ua => ua.Product != null)
                .Where(ua => ua.Product.Name != null)
                .Any(ua => ua.Product.Name.IndexOf("NuGet", StringComparison.InvariantCultureIgnoreCase) != -1);
        }
    }
}