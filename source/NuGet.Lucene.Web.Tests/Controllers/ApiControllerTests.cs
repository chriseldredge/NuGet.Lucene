using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    public abstract class ApiControllerTests<TController> : RouteTests where TController : ApiController
    {
        protected HttpRequestMessage request;
        protected IHttpRoute route;
        protected IHttpRouteData routeData;
        protected TController controller;
        
        [SetUp]
        public void Init()
        {
            controller = CreateController();
            controller.Configuration = configuration;
        }

        protected void SetUpRequest(string routeName, HttpMethod method, string appRelativeUri)
        {
            var absoluteUri = "http://localhost/" + appRelativeUri;

            request = new HttpRequestMessage(method, absoluteUri);
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, configuration);
            request.RequestUri = new Uri(absoluteUri);

            route = configuration.Routes[routeName];
            routeData = route.GetRouteData("/", request);

            Assert.That(routeData, Is.Not.Null, "The route name {0} does not match {1} request uri on {2}.", routeName, method, absoluteUri);

            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = route.GetRouteData("/", request);

            controller.Request = request;
        }

        protected abstract TController CreateController();

        protected void AssertAuthenticationAttributePresent(string action, IEnumerable<Type> expectedMethods)
        {
            var method = controller.GetType().GetMethod(action);

            Assert.That(method, Is.Not.Null, "Action method " + action + " not found on controller type " + controller.GetType());
            var methods = method.GetCustomAttributes(typeof(Attribute), true);

            Assert.That(methods.Select(m => m.GetType()).ToArray(), Is.EquivalentTo(expectedMethods));
        }
    }
}