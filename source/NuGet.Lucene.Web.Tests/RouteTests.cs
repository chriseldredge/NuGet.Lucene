using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using AspNet.WebApi.HtmlMicrodataFormatter;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    public abstract class RouteTests
    {
        protected HttpConfiguration configuration;
        protected HttpRouteCollection routes;
        protected Mock<IDocumentationProviderEx> documentationProvider;

        [SetUp]
        public void BuildRouteTable()
        {
            routes = new HttpRouteCollection();
            configuration = new HttpConfiguration(routes);

            documentationProvider = new Mock<IDocumentationProviderEx>();
            configuration.Services.Replace(typeof(IDocumentationProvider), documentationProvider.Object);
            var routeMapper = new NuGetWebApiRouteMapper("api/");
            routeMapper.MapApiRoutes(configuration);
            routeMapper.MapODataRoutes(configuration);
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