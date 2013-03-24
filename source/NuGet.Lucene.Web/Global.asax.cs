using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.ServiceModel.Activation;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.Routing;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Ninject;
using Ninject.Extensions.Wcf;
using Ninject.Web.Common;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.DataServices;
using NuGet.Lucene.Web.Filters;
using NuGet.Lucene.Web.Formatters;
using NuGet.Lucene.Web.MessageHandlers;
using HttpMethodConstraint = System.Web.Http.Routing.HttpMethodConstraint;

namespace NuGet.Lucene.Web
{
    public class Global : NinjectHttpApplication
    {
        protected override void OnApplicationStarted()
        {
            TaskScheduler.UnobservedTaskException +=
                (_, e) => UnhandledExceptionLogger.LogException(e.Exception,
                    string.Format("Unobserved exception in async task: {0}", e.Exception.Message));

            ConfigureWebApi(GlobalConfiguration.Configuration);

            var hubConfiguration = new HubConfiguration
                {
                    EnableDetailedErrors = ApplicationConfig.ShowExceptionDetails,
                    EnableCrossDomain = ApplicationConfig.EnableCrossDomainRequests
                };

            RouteTable.Routes.MapHubs(hubConfiguration);

            MapApiRoutes(GlobalConfiguration.Configuration);
            MapDataServiceRoutes(RouteTable.Routes);
        }

        protected override IKernel CreateKernel()
        {
            return new StandardKernel(new ApplicationConfig(), new SignalRModule());
        }

        public static void ConfigureWebApi(HttpConfiguration config)
        {
            config.IncludeErrorDetailPolicy = ApplicationConfig.ShowExceptionDetails
                                                  ? IncludeErrorDetailPolicy.Always
                                                  : IncludeErrorDetailPolicy.Default;

            config.MessageHandlers.Add(new CrossOriginMessageHandler(ApplicationConfig.EnableCrossDomainRequests));
            config.Filters.Add(new ExceptionLoggingFilter());
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.Add(new PackageFormDataMediaFormatter());
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
        }

        public static void MapApiRoutes(HttpConfiguration config)
        {
            var routes = config.Routes;
            
            routes.MapHttpRoute(RouteNames.ApiExplorer,
                                "api",
                                new { controller = "ApiExplorer" });

            routes.MapHttpRoute(RouteNames.Indexing,
                                "api/indexing/{action}",
                                new { controller = "Indexing" });

            routes.MapHttpRoute(RouteNames.Users.All,
                                "api/users",
                                new { controller = "Users", action = "GetAllUsers" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Users.ForUser,
                                "api/users/{username}",
                                new { controller = "Users" });

            routes.MapHttpRoute(RouteNames.TabCompletionPackageIds,
                                "api/v2/package-ids",
                                new { controller = "TabCompletion", action = "GetMatchingPackages" });

            routes.MapHttpRoute(RouteNames.TabCompletionPackageVersions,
                                "api/v2/package-versions/{packageId}",
                                new { controller = "TabCompletion", action = "GetPackageVersions" });

            routes.MapHttpRoute(RouteNames.Packages.Search,
                                "api/packages",
                                new { controller = "Packages", action = "Search" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Packages.Upload,
                                "api/packages",
                                new { controller = "Packages" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Put, HttpMethod.Options) });

            var route = routes.MapHttpRoute(RouteNames.Packages.DownloadLatestVersion,
                                "api/packages/{id}/content",
                                new { controller = "Packages", action = "DownloadPackage" });

            AddApiDescription(config, route, typeof(PackagesController), "DownloadPackage", HttpMethod.Get);
            AddApiDescription(config, route, typeof(PackagesController), "DownloadPackage", HttpMethod.Head);

            route = routes.MapHttpRoute(RouteNames.Packages.Download,
                                "api/packages/{id}/{version}/content",
                                new { controller = "Packages", action = "DownloadPackage" },
                                new { version = new SemanticVersionConstraint() });

            AddApiDescription(config, route, typeof (PackagesController), "DownloadPackage", HttpMethod.Get);
            AddApiDescription(config, route, typeof (PackagesController), "DownloadPackage", HttpMethod.Head);

            route = routes.MapHttpRoute(RouteNames.Packages.Info,
                                "api/packages/{id}/{version}",
                                new { controller = "Packages", action = "GetPackageInfo", version = "" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get), version = new OptionalSemanticVersionConstraint() });

            AddApiDescription(config, route, typeof(PackagesController), "GetPackageInfo", HttpMethod.Get);

            route = routes.MapHttpRoute(RouteNames.Packages.Delete,
                                "api/packages/{id}/{version}",
                                new { controller = "Packages", action = "DeletePackage" },
                                new { version = new SemanticVersionConstraint() });

            AddApiDescription(config, route, typeof(PackagesController), "DeletePackage", HttpMethod.Delete);
        }

        private static void AddApiDescription(HttpConfiguration config, IHttpRoute route, Type controllerType, string methodName, HttpMethod method)
        {
            var apiDescriptions = config.Services.GetApiExplorer().ApiDescriptions;
            var controllerDesc = new HttpControllerDescriptor(config, "Packages", controllerType);
            var d = new ApiDescription
                {
                    ActionDescriptor =
                        new ReflectedHttpActionDescriptor(controllerDesc, controllerType.GetMethod(methodName)),
                    HttpMethod = method,
                    Route = route,
                    RelativePath = route.RouteTemplate
                };

            apiDescriptions.Add(d);
        }

        public static void MapDataServiceRoutes(RouteCollection routes)
        {
            var dataServiceHostFactory = new NinjectDataServiceHostFactory();

            var serviceRoute = new ServiceRoute("api/odata", dataServiceHostFactory, typeof(PackageDataService))
                {
                    Defaults = RouteNames.PackageFeedRouteValues,
                    Constraints = RouteNames.PackageFeedRouteValues
                };

            routes.Add(RouteNames.Packages.Feed, serviceRoute);
        }
    }
}