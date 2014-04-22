using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace NuGet.Lucene.Web.OData.Routing.Conventions
{
    /// <summary>
    /// Decorates an <see cref="IODataRoutingConvention"/> to use a controller name
    /// that is different from the default convention.
    /// </summary>
    public class ControllerAliasingODataRoutingConvention : IODataRoutingConvention
    {
        private readonly IODataRoutingConvention delegateRoutingConvention;
        private readonly string controllerAlias;
        private readonly string targetControllerName;

        public ControllerAliasingODataRoutingConvention(IODataRoutingConvention delegateRoutingConvention, string controllerAlias, string targetControllerName)
        {
            this.delegateRoutingConvention = delegateRoutingConvention;
            this.controllerAlias = controllerAlias;
            this.targetControllerName = targetControllerName;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            var controller = delegateRoutingConvention.SelectController(odataPath, request);
            return string.Equals(controller, controllerAlias, StringComparison.OrdinalIgnoreCase)
                ? targetControllerName
                : controller;
        }

        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            return delegateRoutingConvention.SelectAction(odataPath, controllerContext, actionMap);
        }
    }
}