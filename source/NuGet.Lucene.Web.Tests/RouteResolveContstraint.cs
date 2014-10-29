using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NuGet.Lucene.Web.Tests
{
    public class RouteResolveContstraint : Constraint, IResolveConstraint
    {
        private readonly string relativeUrl;
        private readonly HttpMethod method;
        private readonly IDictionary<string, object> expectedRouteValues = new Dictionary<string, object>(); 

        public RouteResolveContstraint(string relativeUrl)
            : this(relativeUrl, HttpMethod.Get)
        {
            this.relativeUrl = relativeUrl;
        }

        public RouteResolveContstraint(string relativeUrl, HttpMethod method)
        {
            this.relativeUrl = relativeUrl;
            this.method = method;
        }

        public Constraint Resolve()
        {
            return this;
        }

        public override bool Matches(object actual)
        {
            var routes = (HttpRouteCollection)actual;

            var message = new HttpRequestMessage(method, "http://localhost/" + relativeUrl);

            var routeData = routes.GetRouteData(message);

            Assert.That(routeData, Is.Not.Null, "RouteData not found for " + relativeUrl);
            
            foreach (var kv in expectedRouteValues)
            {
                object actualValue;

                routeData.Values.TryGetValue(kv.Key, out actualValue);

                Assert.That(actualValue, Is.EqualTo(kv.Value), "RouteData.Values[" + kv.Key + "]");
            }
            return true;
        }

        public override void WriteDescriptionTo(MessageWriter writer)
        {
        }

        public RouteResolveContstraint WithController(string expectedController)
        {
            expectedRouteValues["Controller"] = expectedController;
            return this;
        }

        public RouteResolveContstraint WithAction(string expectedAction)
        {
            expectedRouteValues["Action"] = expectedAction;
            return this;
        }

        public RouteResolveContstraint WithRouteValue(string key, object value)
        {
            expectedRouteValues[key] = value;
            return this;
        }
    }
}