using System;
using System.Net.Http;
using System.ServiceModel.Activation;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.Routing;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Ninject.Extensions.Wcf;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.DataServices;
using HttpMethodConstraint = System.Web.Http.Routing.HttpMethodConstraint;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiRouteMapper
    {
        private readonly string pathPrefix;

        public NuGetWebApiRouteMapper(string pathPrefix)
        {
            this.pathPrefix = pathPrefix;
        }

        public void MapApiRoutes(HttpConfiguration config)
        {
            var routes = config.Routes;

            routes.MapHttpRoute(RouteNames.ApiExplorer,
                                pathPrefix,
                                new { controller = "ApiExplorer" });

            routes.MapHttpRoute(RouteNames.Indexing,
                                pathPrefix + "indexing/{action}",
                                new { controller = "Indexing" });

            routes.MapHttpRoute(RouteNames.Users.All,
                                pathPrefix + "users",
                                new { controller = "Users", action = "GetAllUsers" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Users.ForUser,
                                pathPrefix + "users/{username}",
                                new { controller = "Users" });

            routes.MapHttpRoute(RouteNames.TabCompletionPackageIds,
                                pathPrefix + "v2/package-ids",
                                new { controller = "TabCompletion", action = "GetMatchingPackages" });

            routes.MapHttpRoute(RouteNames.TabCompletionPackageVersions,
                                pathPrefix + "v2/package-versions/{packageId}",
                                new { controller = "TabCompletion", action = "GetPackageVersions" });

            routes.MapHttpRoute(RouteNames.Packages.Search,
                                pathPrefix + "packages",
                                new { controller = "Packages", action = "Search" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Packages.Upload,
                                pathPrefix + "packages",
                                new { controller = "Packages" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Put, HttpMethod.Options) });

            var route = routes.MapHttpRoute(RouteNames.Packages.DownloadLatestVersion,
                                pathPrefix + "packages/{id}/content",
                                new { controller = "Packages", action = "DownloadPackage" });

            AddApiDescription(config, route, typeof(PackagesController), "DownloadPackage", HttpMethod.Get);
            AddApiDescription(config, route, typeof(PackagesController), "DownloadPackage", HttpMethod.Head);

            route = routes.MapHttpRoute(RouteNames.Packages.Download,
                                pathPrefix + "packages/{id}/{version}/content",
                                new { controller = "Packages", action = "DownloadPackage" },
                                new { version = new SemanticVersionConstraint() });

            AddApiDescription(config, route, typeof(PackagesController), "DownloadPackage", HttpMethod.Get);
            AddApiDescription(config, route, typeof(PackagesController), "DownloadPackage", HttpMethod.Head);

            route = routes.MapHttpRoute(RouteNames.Packages.Info,
                                pathPrefix + "packages/{id}/{version}",
                                new { controller = "Packages", action = "GetPackageInfo", version = "" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get), version = new OptionalSemanticVersionConstraint() });

            AddApiDescription(config, route, typeof(PackagesController), "GetPackageInfo", HttpMethod.Get);

            route = routes.MapHttpRoute(RouteNames.Packages.Delete,
                                pathPrefix + "packages/{id}/{version}",
                                new { controller = "Packages", action = "DeletePackage" },
                                new { version = new SemanticVersionConstraint() });

            AddApiDescription(config, route, typeof(PackagesController), "DeletePackage", HttpMethod.Delete);
        }

        public void MapHubs(RouteCollection routes)
        {
            var hubConfiguration = new HubConfiguration
            {
                EnableDetailedErrors = NuGetWebApiModule.ShowExceptionDetails,
                EnableCrossDomain = NuGetWebApiModule.EnableCrossDomainRequests,
            };

            routes.MapHubs(SignalrRoutePath, hubConfiguration);
        }

        public void MapDataServiceRoutes(RouteCollection routes)
        {
            var dataServiceHostFactory = new NinjectDataServiceHostFactory();

            var serviceRoute = new ServiceRoute(ODataRoutePath, dataServiceHostFactory, typeof(PackageDataService))
            {
                Defaults = RouteNames.PackageFeedRouteValues,
                Constraints = RouteNames.PackageFeedRouteValues
            };

            routes.Add(RouteNames.Packages.Feed, serviceRoute);
        }

        public void AddApiDescription(HttpConfiguration config, IHttpRoute route, Type controllerType, string methodName, HttpMethod method)
        {
            var apiDescriptions = config.Services.GetApiExplorer().ApiDescriptions;
            var controllerDesc = new HttpControllerDescriptor(config, "Packages", controllerType);
            var api = new ApiDescription
            {
                ActionDescriptor =
                    new ReflectedHttpActionDescriptor(controllerDesc, controllerType.GetMethod(methodName)),
                HttpMethod = method,
                Route = route,
                RelativePath = route.RouteTemplate
            };

            apiDescriptions.Add(api);
        }

        public string PathPrefix { get { return pathPrefix; } }
        public string ODataRoutePath { get { return PathPrefix + "odata"; } }
        public string SignalrRoutePath { get { return PathPrefix + "signalr"; } }
    }
}