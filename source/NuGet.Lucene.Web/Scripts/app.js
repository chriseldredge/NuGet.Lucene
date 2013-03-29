define([
        'ember',
        'config',
        'restapi',
        'Controllers/AdminController',
        'Controllers/ApplicationController',
        'Controllers/SearchController',
        'Models/PackageIndexer',
        'Models/PackageStore',
        'Models/RestClient',
        'Routes/AdminRoute',
        'Routes/SearchRoute',
        'Routes/ViewPackageRoute',
        'Views/Footer',
        'Views/PackageIcon'
], function (em, config, restapi, AdminController, ApplicationController, SearchController, PackageIndexer, PackageStore, RestClient, AdminRoute, SearchRoute, ViewPackageRoute, Footer, PackageIcon) {

    var app = em.Application.create({name: "NuGet"});
    app.deferReadiness();
    
    restapi.then(function () {
        app.advanceReadiness();
    });

    app.RestClient = RestClient.create({
        baseUrl: config.baseDataUrl,
        apiKey: config.apiKey,
    });

    app.PackageIndexer = PackageIndexer.create({
        restClient: app.RestClient,
    });
    
    app.Packages = PackageStore.create({
        restClient: app.RestClient
    });

    app.AdminController = AdminController;
    app.ApplicationController = ApplicationController;
    app.SearchController = SearchController;
    
    app.AdminRoute = AdminRoute;
    app.SearchRoute = SearchRoute;
    app.ViewPackageRoute = ViewPackageRoute;

    app.Footer = Footer;
    app.PackageIcon = PackageIcon;

    return app;
});