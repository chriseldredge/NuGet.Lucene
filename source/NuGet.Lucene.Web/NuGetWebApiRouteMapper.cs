using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Http.Routing;
using NuGet.Lucene.Web.OData.Formatter.Serialization;
using NuGet.Lucene.Web.OData.Routing;
using NuGet.Lucene.Web.OData.Routing.Conventions;

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
        /// Adds route handlers that will redirect NuGet User Agents so that
        /// a single source URL can be used in the client.
        /// </summary>
        /// <param name="config">
        /// The configuration in which to register routes
        /// </param>
        public void MapNuGetClientRedirectRoutes(HttpConfiguration config)
        {
            // redirect NuGet user agents from / to /api/odata
            config.Routes.MapHttpRoute(RouteNames.Redirect.RootFeed,
                                string.Empty,
                                new { },
                                new { userAgent = new NuGetUserAgentConstraint() },
                                new ODataRedirectHandler(RouteNames.Packages.Feed));

            // redirect NuGet user agents from /api to /api/odata
            config.Routes.MapHttpRoute(RouteNames.Redirect.ApiFeed,
                                pathPrefix,
                                new { },
                                new { userAgent = new NuGetUserAgentConstraint() },
                                new ODataRedirectHandler(RouteNames.Packages.Feed));

            // redirect e.g. PUT /api/odata to PUT /api/packages
            config.Routes.MapHttpRoute(RouteNames.Redirect.Upload,
                                ODataRoutePath,
                                new { },
                                new { method = new HttpMethodConstraint(HttpMethod.Put) },
                                new RedirectHandler(RouteNames.Packages.Upload));

            // redirect e.g. DELETE /api/odata/Foo/1.0 to DELETE /api/packages/Foo/1.0
            config.Routes.MapHttpRoute(RouteNames.Redirect.Delete,
                                ODataRoutePath + "/{id}/{version}",
                                new { },
                                new { method = new HttpMethodConstraint(HttpMethod.Delete) },
                                new RedirectHandler(RouteNames.Packages.Delete));
        }

        [Obsolete("Use overload without routeTemplate")]
        public void MapNuGetClientRedirectRoutes(HttpConfiguration config, string routeTemplate)
        {
            MapNuGetClientRedirectRoutes(config);
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

            routes.MapHttpRoute(RouteNames.Packages.GetAvailableSearchFieldNames,
                                pathPrefix + "packages/$searchable-fields",
                                new { controller = "Packages", action = "GetAvailableSearchFieldNames" },
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
                                new { controller = "Packages", action = "GetPackageInfo", version = RouteParameter.Optional },
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

            routes.MapHttpRoute(RouteNames.Symbols.Upload,
                                pathPrefix + "symbols",
                                new { controller = "Symbols" },
                                new { httpMethod = new HttpMethodConstraint(HttpMethod.Put, HttpMethod.Options) });

            routes.MapHttpRoute(RouteNames.Symbols.GetFile,
                        pathPrefix + "symbols/{*path}",
                        new { controller = "Symbols", action = "GetFile" });
        }

        public void MapODataRoutes(HttpConfiguration config)
        {
            var builder = new NuGetWebApiODataModelBuilder();

            builder.Build();

            config.Formatters.InsertRange(0,
                ODataMediaTypeFormatters.Create(
                    new ODataPackageDefaultStreamAwareSerializerProvider(),
                    new DefaultODataDeserializerProvider()));

            var conventions = new List<IODataRoutingConvention>
            {
                new CompositeKeyRoutingConvention(),
                new NonBindableActionRoutingConvention("PackagesOData"),
                new EntitySetCountRoutingConvention(),
                new NonBindableActionCountRoutingConvention("PackagesOData")
            };

            conventions.AddRange(ODataRoutingConventions.CreateDefault());

            conventions = conventions.Select(c => new ControllerAliasingODataRoutingConvention(c, "Packages", "PackagesOData")).Cast<IODataRoutingConvention>().ToList();

            config.Routes.MapODataServiceRoute(
                RouteNames.Packages.Feed,
                ODataRoutePath,
                builder.Model,
                new CountODataPathHandler(),
                conventions);
        }

        public string PathPrefix { get { return pathPrefix; } }
        public string ODataRoutePath { get { return PathPrefix + "odata"; } }
        public string SignalrRoutePath { get { return PathPrefix + "signalr"; } }
    }
}
