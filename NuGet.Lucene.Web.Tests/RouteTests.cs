using System.Net.Http;
using System.Web.Http;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    public abstract class RouteTests
    {
        protected HttpConfiguration configuration;
        protected HttpRouteCollection routes;

        [SetUp]
        public void BuildRouteTable()
        {
            routes = new HttpRouteCollection();
            configuration = new HttpConfiguration(routes);
            Global.MapApiRoutes(routes);
        }

        public RouteResolveContstraint HasRouteFor(string url)
        {
            return new RouteResolveContstraint(url);
        }

        public RouteResolveContstraint HasRouteFor(string url, HttpMethod method)
        {
            return new RouteResolveContstraint(url, method);
        }
    }
}