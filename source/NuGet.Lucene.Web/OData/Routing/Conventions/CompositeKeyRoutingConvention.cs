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

            controllerContext.RouteData.DecomposeKey();

            return action;
        }
    }
}
