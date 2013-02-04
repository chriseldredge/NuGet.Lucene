using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Ninject;
using Ninject.Extensions.Wcf;
using Ninject.Web.Common;
using NuGet.Lucene.Web.DataServices;
using NuGet.Lucene.Web.Filters;
using NuGet.Lucene.Web.Formatters;

namespace NuGet.Lucene.Web
{
    public class Global : NinjectHttpApplication
    {
        public const string PackageFeedRouteName = "OData Package Feed";
        public static readonly RouteValueDictionary PackageFeedRouteValues = new RouteValueDictionary { { "serviceType", "odata" } };

        protected override void OnApplicationStarted()
        {
            TaskScheduler.UnobservedTaskException +=
                (_, e) => UnhandledExceptionLogger.Log.Fatal(
                    m => m("Unobserved exception in async task: {0}", e.Exception.Message), e.Exception);

            MapMvcRoutes(RouteTable.Routes);
            MapApiRoutes(RouteTable.Routes);
            MapDataServiceRoutes(RouteTable.Routes);

            GlobalConfiguration.Configuration.Filters.Add(new ExceptionLoggingFilter());
            GlobalConfiguration.Configuration.Formatters.Add(new PackageFormDataMediaFormatter());
        }

        protected override IKernel CreateKernel()
        {
            return new StandardKernel(new ApplicationConfig());
        }

        public static void MapMvcRoutes(RouteCollection routes)
        {
            routes.MapRoute("Error Test",
                            "error/throw/{statusCode}",
                            new { controller = "Error", action = "Throw", statusCode = 500 });

            routes.MapRoute("Error",
                            "error/{statusCode}",
                            new { controller = "Error", action = "HandleError", statusCode = 500 },
                            new { statusCode = @"\d\d\d"});
        }

        public static void MapApiRoutes(RouteCollection routes)
        {
            routes.MapHttpRoute("Home", "",
                                new { controller = "Home", action = "Redirect" });

            routes.MapHttpRoute("Status", "api/status",
                                new {controller = "Indexing", action = "Status"});

            routes.MapHttpRoute("PackageApi", "api/v2/package/{id}/{version}",
                                new {controller = "Packages", id = UrlParameter.Optional, version = UrlParameter.Optional});

            routes.MapHttpRoute("PackageApi-Upload-Alternate", "api/v2",
                                new { controller = "Packages" },
                                new { httpMethod = new HttpMethodConstraint("PUT", "POST")});

            routes.MapHttpRoute("PackageApi-Delete-Alternate", "api/v2/{id}/{version}",
                                new { controller = "Packages" },
                                new { httpMethod = new HttpMethodConstraint("DELETE") });

            routes.MapHttpRoute("Package Manager Console Tab Completion - Package IDs",
                                "api/v2/package-ids",
                                new { controller = "TabCompletion", action = "GetMatchingPackages", maxResults = 30, includePrerelease = false });

            routes.MapHttpRoute("Package Manager Console Tab Completion - Package Versions",
                                "api/v2/package-versions/{packageId}",
                                new {controller = "TabCompletion", action = "GetPackageVersions", includePrerelease = false});

            routes.MapHttpRoute("UserApi", "api/users/{username}",
                                new {controller = "User", username = RouteParameter.Optional});

        }

        public static void MapDataServiceRoutes(RouteCollection routes)
        {
            var dataServiceHostFactory = new NinjectDataServiceHostFactory();
            
            var serviceRoute = new ServiceRoute("api/v2", dataServiceHostFactory, typeof(PackageDataService))
                {
                    Defaults = PackageFeedRouteValues,
                    Constraints = PackageFeedRouteValues
                };
            
            routes.Add(PackageFeedRouteName, serviceRoute);
        }
    }
}