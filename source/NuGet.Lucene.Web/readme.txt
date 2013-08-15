## NuGet.Lucene.Web - Overview

This package provides controllers and WCF Data Services
classes for exposing a NuGet.Lucene package repository
over OData.

## Getting Started

Ninject is used for configuration and dependency injection
of the controllers and routes. In your Ninject bootstrapping
code, create a Kernel as follows:

    var kernel = new StandardKernel(new NuGetWebApiModule(), new SignalRModule());

The SignalRModule is optional and only required if you want
near real-time updates when packages are added or removed.

Next, in your start up code (Global.asax), register API routes
as follows:

    protected void Application_Start(object sender, EventArgs e)
    {
        var routeMapper = kernel.Get<NuGetWebApiRouteMapper>();
        routeMapper.MapApiRoutes(GlobalConfiguration.Configuration);
        routeMapper.MapDataServiceRoutes(RouteTable.Routes);

        // Optional if using SignalR:
        routeMapper.MapHubs(RouteTable.Routes);
    }

## HtmlMicrodataFormatter

This project depends on AspNet.WebApi.HtmlMicrodataFormatter to format responses
using html5 and microdata. To enable this functionality, add the following
to your start up code:

    public static void ConfigureWebApi(HttpConfiguration config)
	{
	    // load xml documentation for assemblies
        var documentation = new HtmlDocumentation();
        documentation.Load();
        config.Services.Replace(typeof(IDocumentationProvider), new WebApiHtmlDocumentationProvider(documentation));

		// register the formatter
        config.Formatters.Add(new NuGetHtmlMicrodataFormatter());
    }


## Configuration

The following appSetting keys configure the module:

* NuGet.Lucene.Web:showExceptionDetails flag indicating if stack traces should be shown to remote clients. May disclose sensitive
information when enabled. Default: false.
* NuGet.Lucene.Web:enableCrossDomainRequests flag indicating if CORS headers should be sent to enable cross-domain requests to be sent by browsers. Default: false
* NuGet.Lucene.Web:routePathPrefix Path where controller routes are registered. Default: /api
* NuGet.Lucene.Web:packageMirrorTargetUrl When non-blank, when a client attempts to install or restore a package that isn't in the Lucene index, it will automatically be mirrored from this remote feed. Provides automatic mirroring of remote packages during package restore. Default: unset (disabled)
* NuGet.Lucene.Web:requireApiKey flag indicating if clients must authenticate with an api key when pushing or deleting packages. Default: true
* NuGet.Lucene.Web:packagesPath Location where package files are stored (may be a virtual path). Default: ~/App_Data/Packages
* NuGet.Lucene.Web:lucenePath Location where Lucene index files are stored (may be a virtual path). Default: ~/App_Data/Lucene
* NuGet.Lucene.Web:enablePackageFileWatcher flag indicating if a file system watcher should monitor the packages path for changes to keep the index in sync. Use this setting if any external process adds or removes package files. Default: true
* NuGet.Lucene.Web:synchronizeOnStart when true, scans the packagesPath and compares nupkg files with the Lucene index and updates the Lucene index to match the file system. This setting enables the Lucene index to be kept in sync when package files change while the web app isn't running. Default: true
* NuGet.Lucene.Web:groupPackageFilesById when true, package files are stored into subdirectories by package ID. When false, all packages are stored in the top-level packages path. Default: true
