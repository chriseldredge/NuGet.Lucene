using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData.Routing;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web.OData.Routing.Conventions
{
    public static class HttpRouteDataExtensions
    {
        public static void DecomposeKey(this IHttpRouteData routeData)
        {
            var routeValues = routeData.Values;
            object value;

            if (!routeValues.TryGetValue(ODataRouteConstants.Key, out value)) return;

            var compoundKeyPairs = ((string)value).Split(',');

            if (!compoundKeyPairs.Any())
            {
                return;
            }

            var keyValues = compoundKeyPairs.Select(kv => kv.Split('=')).Select(kv => new KeyValuePair<string, object>(kv[0], kv[1]));

            routeValues.AddRange(keyValues);
        }
    }
}