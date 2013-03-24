using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web
{
    public class SemanticVersionConstraint : IHttpRouteConstraint
    {
        public virtual bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName,
                          IDictionary<string, object> values,
                          HttpRouteDirection routeDirection)
        {
            object str;

            if (values.TryGetValue("version", out str))
            {
                SemanticVersion version;
                return SemanticVersion.TryParse(str.ToString(), out version);
            }

            return false;
        }
    }
}