using System.Net.Http;
using System.ServiceModel.Activation;
using System.Web.Http;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Routing;
using Microsoft.Data.OData;
using Ninject.Extensions.Wcf;
using NuGet.Lucene.Web.DataServices;
using NuGet.Lucene.Web.Models;
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

        /// <summary>
        /// See <see cref="MapNuGetClientRedirectRoutes(System.Web.Http.HttpConfiguration, string)"/>
        /// </summary>
        public void MapNuGetClientRedirectRoutes(HttpConfiguration config)
        {
            MapNuGetClientRedirectRoutes(config, string.Empty);
        }

        /// <summary>
        /// Adds route handlers that will redirect NuGet User Agents so that
        /// a single source URL can be used in the client.
        /// </summary>
        /// <param name="config">
        /// The configuration in which to register routes
        /// </param>
        /// <param name="routeTemplate">
        /// Where the primary redirect should take place.
        /// The empty string should be used in most cases, but if you want to
        /// point your clients to http://foo/api you could use <c>"api"</c> instead.
        /// </param>
        public void MapNuGetClientRedirectRoutes(HttpConfiguration config, string routeTemplate)
        {
            config.Routes.MapHttpRoute(RouteNames.Redirect.Feed,
                                routeTemplate,
                                new { },
                                new { userAgent = new NuGetUserAgentConstraint() },
                                new RedirectHandler(RouteNames.Packages.Feed, RouteNames.PackageFeedRouteValues) { AppendTrailingSlash = true });

            config.Routes.MapHttpRoute(RouteNames.Redirect.Upload,
                                ODataRoutePath,
                                new { },
                                new { method = new HttpMethodConstraint(HttpMethod.Put) },
                                new RedirectHandler(RouteNames.Packages.Upload));

            config.Routes.MapHttpRoute(RouteNames.Redirect.Delete,
                                ODataRoutePath + "/{id}/{version}",
                                new { },
                                new { method = new HttpMethodConstraint(HttpMethod.Delete) },
                                new DeletePackageRedirectHandler());
        }

        public void MapApiRoutes(HttpConfiguration config)
        {
            var routes = config.Routes;
            
            routes.MapHttpRoute(AspNet.WebApi.HtmlMicrodataFormatter.RouteNames.ApiDocumentation,
                                pathPrefix,
                                new { controller = "NuGetDocumentation", action = "GetApiDocumentation" });

            routes.MapHttpRoute(AspNet.WebApi.HtmlMicrodataFormatter.RouteNames.TypeDocumentation,
                                pathPrefix + "schema/{typeName}",
                                new { controller = "NuGetDocumentation", action = "GetTypeDocumentation" });
            
            routes.MapHttpRoute(RouteNames.Indexing,
                                pathPrefix + "indexing/{action}",
                                new { controller = "Indexing" });
            
            routes.MapHttpRoute(RouteNames.Users.All,
                                pathPrefix + "users",
                                new { controller = "Users", action = "GetAllUsers" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Users.GetUser,
                                pathPrefix + "users/{*username}",
                                new { controller = "Users", action = "Get" },
                                new { username = ".+", method = new HttpMethodConstraint(HttpMethod.Get, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Users.PutUser,
                                pathPrefix + "users/{*username}",
                                new { controller = "Users", action = "Put" },
                                new { username = ".+", method = new HttpMethodConstraint(HttpMethod.Put, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Users.PostUser,
                                pathPrefix + "users/{*username}",
                                new { controller = "Users", action = "Post" },
                                new { username = ".+", method = new HttpMethodConstraint(HttpMethod.Post, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Users.DeleteUser,
                                pathPrefix + "users/{*username}",
                                new { controller = "Users", action = "Delete" },
                                new { username = ".+", method = new HttpMethodConstraint(HttpMethod.Delete, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Users.DeleteAll,
                                pathPrefix + "users",
                                new { controller = "Users", action = "DeleteAllUsers" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Delete) });

            routes.MapHttpRoute(RouteNames.Users.GetAuthenticationInfo,
                                pathPrefix + "session",
                                new { controller = "Users", action = "GetAuthenticationInfo" });

            routes.MapHttpRoute(RouteNames.Users.ChangeApiKey,
                                pathPrefix + "session/changeApiKey",
                                new { controller = "Users", action = "ChangeApiKey" });

            routes.MapHttpRoute(RouteNames.Users.GetRequiredAuthenticationInfo,
                                pathPrefix + "authenticate",
                                new { controller = "Users", action = "GetRequiredAuthenticationInfo" });
            
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
            
            routes.MapHttpRoute(RouteNames.Packages.DownloadLatestVersion,
                                pathPrefix + "packages/{id}/content",
                                new { controller = "Packages", action = "DownloadPackage" });

            routes.MapHttpRoute(RouteNames.Packages.Download,
                                pathPrefix + "packages/{id}/{version}/content",
                                new { controller = "Packages", action = "DownloadPackage" },
                                new { version = new SemanticVersionConstraint() });

            routes.MapHttpRoute(RouteNames.Packages.Info,
                                pathPrefix + "packages/{id}/{version}",
                                new { controller = "Packages", action = "GetPackageInfo", version = "" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get), version = new OptionalSemanticVersionConstraint() });
            
            routes.MapHttpRoute(RouteNames.Packages.Delete,
                                pathPrefix + "packages/{id}/{version}",
                                new { controller = "Packages", action = "DeletePackage" },
                                new { version = new SemanticVersionConstraint() });
        }

        public void MapSymbolSourceRoutes(HttpConfiguration config)
        {
            var routes = config.Routes;

            routes.MapHttpRoute(RouteNames.Sources,
                                pathPrefix + "source/{id}/{version}/{*path}",
                                new { controller = "SourceFiles" },
                                new { version = new SemanticVersionConstraint() });

            routes.MapHttpRoute(RouteNames.Symbols.Settings,
                        pathPrefix + "symbol-settings",
                        new { controller = "Symbols", action = "GetSettings" });

            routes.MapHttpRoute(RouteNames.Symbols.GetFile,
                        pathPrefix + "symbols/{*path}",
                        new { controller = "Symbols", action = "GetFile" });
        }

        public void MapDataServiceRoutes(HttpConfiguration config)
        {
            var builder = new ODataConventionModelBuilder();
            var entity = builder.EntitySet<ODataPackage>("PackagesOData");
            entity.EntityType.HasKey(pkg => pkg.Id);
            entity.EntityType.HasKey(pkg => pkg.Version);
            
            //ActionConfiguration rateProduct = builder.Entity<Product>().Action("RateProduct");
            //rateProduct.Parameter<int>("Rating");
            //rateProduct.Returns<double>();

            config.Formatters.InsertRange(0,
                ODataMediaTypeFormatters.Create(
                    new NamedStreamAwareSerializerProvider(),
                    new DefaultODataDeserializerProvider()));

            config.Routes.MapODataRoute(RouteNames.Packages.Feed, ODataRoutePath, builder.GetEdmModel());

            /*
            var dataServiceHostFactory = new NinjectDataServiceHostFactory();

            var serviceRoute = new ServiceRoute(ODataRoutePath, dataServiceHostFactory, typeof(PackageDataService))
            {
                Defaults = RouteNames.PackageFeedRouteValues,
                Constraints = RouteNames.PackageFeedRouteValues
            };

            routes.Add(RouteNames.Packages.Feed, serviceRoute);
            */
        }

        public string PathPrefix { get { return pathPrefix; } }
        public string ODataRoutePath { get { return PathPrefix + "odata"; } }
        public string SignalrRoutePath { get { return PathPrefix + "signalr"; } }
    }
}