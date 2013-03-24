define([
        'ember',
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
], function (em, AdminController, ApplicationController, SearchController, PackageIndexer, PackageStore, RestClient, AdminRoute, SearchRoute, ViewPackageRoute, Footer, PackageIcon) {

    var app = em.Application.create({name: "NuGet"});
    app.deferReadiness();

    app.RestClient = RestClient.create({
        baseUrl: BaseDataUrl,
        apiKey: ApiKey,
        ready: function () {
            app.advanceReadiness();
        }
    });
    
    app.PackageIndexer = PackageIndexer.create({
        restClient: app.RestClient
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