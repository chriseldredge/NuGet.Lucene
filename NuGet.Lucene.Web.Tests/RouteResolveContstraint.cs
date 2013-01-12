using System.Collections.Generic;
using System.Web;
using System.Web.Routing;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NuGet.Lucene.Web.Tests
{
    public class RouteResolveContstraint : Constraint, IResolveConstraint
    {
        private readonly string url;
        private readonly string method;
        private readonly IDictionary<string, object> expectedRouteValues = new Dictionary<string, object>(); 

        public RouteResolveContstraint(string url)
            : this(url, "get")
        {
            this.url = url;
        }

        public RouteResolveContstraint(string url, string method)
        {
            this.url = url;
            this.method = method;
        }

        public Constraint Resolve()
        {
            return this;
        }

        public override bool Matches(object actual)
        {
            var routes = (RouteCollection)actual;

            var context = new Mock<HttpContextBase>();
            context.Setup(c => c.Request.HttpMethod).Returns(method);
            context.Setup(c => c.Request.AppRelativeCurrentExecutionFilePath).Returns(url);

            var routeData = routes.GetRouteData(context.Object);

            Assert.That(routeData, Is.Not.Null, "RouteData not found for " + url);

            foreach (var kv in expectedRouteValues)
            {
                Assert.That(routeData.Values[kv.Key], Is.EqualTo(kv.Value), "RouteData.Values[" + kv.Key + "]");
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