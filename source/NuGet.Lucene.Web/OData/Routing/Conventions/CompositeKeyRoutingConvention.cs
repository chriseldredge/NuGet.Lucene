using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace NuGet.Lucene.Web.OData.Routing.Conventions
{
    /// <summary>
    /// Enables OData entities to be retrieved by URIs that use composite keys
    /// as in <c>~/odata/Packages(Id='Foo',Version='1.0')</c>.
    /// </summary>
    public class CompositeKeyRoutingConvention : IODataRoutingConvention
    {
        private readonly EntityRoutingConvention entityRoutingConvention = new EntityRoutingConvention();

        public virtual string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            return entityRoutingConvention.SelectController(odataPath, request);
        }

        public virtual string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            var action = entityRoutingConvention.SelectAction(odataPath, controllerContext, actionMap);
            if (action == null)
            {
                return null;
            }

            var routeValues = controllerContext.RouteData.Values;
            object value;
            if (!routeValues.TryGetValue(ODataRouteConstants.Key, out value)) return action;

            var compoundKeyPairs = ((string)value).Split(',');

            if (!compoundKeyPairs.Any())
            {
                return null;
            }

            var keyValues = compoundKeyPairs.Select(kv => kv.Split('=')).Select(kv => new KeyValuePair<string, object>(kv[0], kv[1]));

            routeValues.AddRange(keyValues);

            return action;
        }
    }
}