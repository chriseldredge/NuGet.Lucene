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

        // Optional: redirect NuGet client / Visual Studio from ~/ to ~/api/odata:
        routeMapper.MapNuGetClientRedirectRoutes(GlobalConfiguration.Configuration);

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
* NuGet.Lucene.Web:symbolsPath Location where symbol package contents are stored (may be a virtual path). Default: ~/App_Data/Symbols
* NuGet.Lucene.Web:DebuggingToolsPath Location where Debugging Tools for Windows is installed. Used for processing symbol packages. No default.
* NuGet.Lucene.Web:keepSourcesCompressed flag indicating if symbol packages should be eagerly unzipped or if source files should only be unzipped on demand. Default: true
* NuGet.Lucene.Web:enablePackageFileWatcher flag indicating if a file system watcher should monitor the packages path for changes to keep the index in sync. Use this setting if any external process adds or removes package files. Default: true
* NuGet.Lucene.Web:synchronizeOnStart when true, scans the packagesPath and compares nupkg files with the Lucene index and updates the Lucene index to match the file system. This setting enables the Lucene index to be kept in sync when package files change while the web app isn't running. Default: true
* NuGet.Lucene.Web:groupPackageFilesById when true, package files are stored into subdirectories by package ID. When false, all packages are stored in the top-level packages path. Default: true
* NuGet.Lucene.Web:packageOverwriteMode specifies if packages with the same id and version can be overwritten. One of Allow or Deny; Default: Allow
* NuGet.Lucene.Web:handleLocalRequestsAsAdmin when true, requests on local interfaces (127.0.0.1, ::1) are automatically granted administrative permissions
* NuGet.Lucene.Web:localAdministratorApiKey when non-blank, sets the api key on the LocalAdministrator account to a specific value instead of generating one
* NuGet.Lucene.Web:allowAnonymousPackageChanges when true, does not require an api key or other authentication to push and delete packages
* NuGet.Lucene.Web:alwaysCheckMirror when true, always check mirror for packages when offering new versions. When false, only check mirror when package is not in local repository or local packages were all previously mirrored.

### Role mapping

NuGet.Lucene.Web uses the roles AccountAdministrator and PackageManager to control
access to methods that manage user accounts and upload and delete packages, respectively.

For enterprises using Windows authentication you may have groups that you want to alias
to these roles instead of creating new security groups in Active Directory.

To do this, define the roleMappings config section with your security groups:

```xml
<configuration>
  <configSections>
    <sectionGroup name="common">
      <section name="logging" type="Common.Logging.ConfigurationSectionHandler, Common.Logging" />
    </sectionGroup>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
    <section name="roleMappings" type="System.Configuration.NameValueSectionHandler" />
  </configSections>
  <appSettings>
  <roleMappings>
    <add key="PackageManager" value="DOMAIN\Developers"/>
    <add key="AccountAdministrator" value="DOMAIN\Administrators"/>
  </roleMappings>
</configuration>
```

Multiple roles can be specified in the value, delimited by commas.
If multiple roles are specified, the user only needs to be in one
of the roles to be considered a member of the target role.
