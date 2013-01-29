using System.Data.Services;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Ninject;
using Ninject.Extensions.Wcf;
using Ninject.Web.Common;
using NuGet.Lucene.Web.DataServices;

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
            MapDataServiceRoutes(RouteTable.Routes);
        }

        protected override IKernel CreateKernel()
        {
            return new StandardKernel(new ApplicationConfig());
        }

        public static void MapMvcRoutes(RouteCollection routes)
        {
            routes.MapRoute("Default", "",
                            new { controller = "Home", action = "Redirect" });

            routes.MapRoute("Status", "status",
                            new {controller = "Packages", action = "Status"});

            routes.MapRoute("Upload Package", "api/v2",
                            new { controller = "Packages", action = "Upload" },
                            new { httpMethod = new HttpMethodConstraint("PUT", "POST") });

            routes.MapRoute("Delete Package", "api/v2/{id}/{version}",
                            new { controller = "Packages", action = "Delete" },
                            new { httpMethod = new HttpMethodConstraint("DELETE") });

            routes.MapRoute("Download Package", "api/v2/package/{id}/{version}",
                            new { controller = "Packages", action = "Download", version = UrlParameter.Optional });

            routes.MapRoute("Package Manager Console Tab Completion - Package IDs", "api/v2/package-ids",
                            new { controller = "TabCompletion", action = "GetMatchingPackages", maxResults = 30 });

            routes.MapRoute("Package Manager Console Tab Completion - Package Versions",
                            "api/v2/package-versions/{packageId}",
                            new { controller = "TabCompletion", action = "GetPackageVersions" });

            routes.MapRoute("Create User", "users",
                new { controller = "User", action = "Create" },
                new { httpMethod = new HttpMethodConstraint("PUT", "POST") });

            routes.MapRoute("Error Test",
                            "error/throw/{statusCode}",
                            new { controller = "Error", action = "Throw", statusCode = 500 });

            routes.MapRoute("Error",
                            "error/{statusCode}",
                            new { controller = "Error", action = "HandleError", statusCode = 500 },
                            new { statusCode = @"\d\d\d"});
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