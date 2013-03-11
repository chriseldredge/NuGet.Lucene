using System.Net.Http;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Ninject;
using Ninject.Extensions.Wcf;
using Ninject.Web.Common;
using NuGet.Lucene.Web.DataServices;
using NuGet.Lucene.Web.Filters;
using NuGet.Lucene.Web.Formatters;
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

            var hubConfiguration = new HubConfiguration {EnableDetailedErrors = ApplicationConfig.ShowExceptionDetails};

            RouteTable.Routes.MapHubs(hubConfiguration);
            
            MapApiRoutes(GlobalConfiguration.Configuration.Routes);
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

            config.Filters.Add(new ExceptionLoggingFilter());
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.Add(new PackageFormDataMediaFormatter());
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
        }

        public static void MapApiRoutes(HttpRouteCollection routes)
        {
            routes.MapHttpRoute(RouteNames.Home,
                                "",
                                new { controller = "Home" },
                                new { acceptHeader = new AcceptHtmlConstraint() });

            routes.MapHttpRoute(RouteNames.IndexingStatus,
                                "api/indexing/{action}",
                                new {controller = "Indexing", action = "Status"});
            
            routes.MapHttpRoute(RouteNames.UserApi,
                                "api/users/{username}",
                                new { controller = "User", username = RouteParameter.Optional });

            routes.MapHttpRoute(RouteNames.PackageSearch,
                                "api/v2/package",
                                new { controller = "Packages", action = "Search" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) });

            routes.MapHttpRoute(RouteNames.PackageDownloadAnyVersion,
                                "api/v2/package/{id}/content",
                                new { controller = "Packages", action = "GetPackageInfo" });

            routes.MapHttpRoute(RouteNames.PackageDownload,
                                "api/v2/package/{id}/{version}/content",
                                new { controller = "Packages", action = "DownloadPackage" });

            routes.MapHttpRoute(RouteNames.PackageInfo,
                                "api/v2/package/{id}/{version}",
                                new {controller = "Packages", action = "GetPackageInfo", version = ""},
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) });
            
            routes.MapHttpRoute(RouteNames.PackageApi,
                                "api/v2/package/{id}/{version}",
                                new { controller = "Packages", id = "", version = "" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Put, HttpMethod.Post, HttpMethod.Delete) });
            
            routes.MapHttpRoute(RouteNames.TabCompletionPackageIds,
                                "api/v2/package-ids",
                                new { controller = "TabCompletion", action = "GetMatchingPackages", maxResults = 30, includePrerelease = false });

            routes.MapHttpRoute(RouteNames.TabCompletionPackageVersions,
                                "api/v2/package-versions/{packageId}",
                                new {controller = "TabCompletion", action = "GetPackageVersions", includePrerelease = false});
        }

        public static void MapDataServiceRoutes(RouteCollection routes)
        {
            var dataServiceHostFactory = new NinjectDataServiceHostFactory();
            
            var serviceRoute = new ServiceRoute("", dataServiceHostFactory, typeof(PackageDataService))
                {
                    Defaults = RouteNames.PackageFeedRouteValues,
                    Constraints = RouteNames.PackageFeedRouteValues
                };
            
            routes.Add(RouteNames.PackageFeed, serviceRoute);
        }
    }

}