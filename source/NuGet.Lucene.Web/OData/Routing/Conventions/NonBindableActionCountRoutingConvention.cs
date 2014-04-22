using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace NuGet.Lucene.Web.OData.Routing.Conventions
{
    /// <summary>
    /// Adds support for $count operation on non-bindable actions when controller has a <c>"Count" + ActionName</c> action.
    /// </summary>
    public class NonBindableActionCountRoutingConvention : IODataRoutingConvention
    {
        private readonly string controllerName;

        public NonBindableActionCountRoutingConvention(string controllerName)
        {
            this.controllerName = controllerName;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            if (odataPath.PathTemplate == "~/action/$count")
            {
                return controllerName;
            }
            return null;
        }

        // Route the action to a method with the same name as the action.
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (odataPath.PathTemplate != "~/action/$count")
            {
                return null;
            }

            var actionSegment = odataPath.Segments.OfType<ActionPathSegment>().Single();
            var action = actionSegment.Action;

            if (action.IsBindable)
            {
                return null;
            }

            var actionName = "Count" + action.Name;

            if (actionMap.Contains(actionName) && actionMap[actionName].Any(desc => MatchHttpMethod(desc, controllerContext.Request.Method)))
            {
                return actionName;
            }

            return null;
        }

        private bool MatchHttpMethod(HttpActionDescriptor desc, HttpMethod method)
        {
            var supportedMethods = desc.ActionBinding.ActionDescriptor.SupportedHttpMethods;
            return supportedMethods.Contains(method);
        }
    }
}