using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web
{
    public class OptionalSemanticVersionConstraint : SemanticVersionConstraint
    {
        public override bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            object version;
            if (!values.TryGetValue("version", out version) || string.IsNullOrEmpty(version as string)) return true;
            return base.Match(request, route, parameterName, values, routeDirection);
        }
    }
}