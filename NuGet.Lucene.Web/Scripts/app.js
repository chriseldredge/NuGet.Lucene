define([
        'ember',
        'Controllers/AdminController',
        'Controllers/ApplicationController',
        'Controllers/SearchController',
        'Models/IndexingModel',
        'Models/PackageStore',
        'Routes/AdminRoute',
        'Routes/SearchRoute',
        'Routes/ViewPackageRoute',
        'Views/Footer',
        'Views/PackageIcon'
], function (em, AdminController, ApplicationController, SearchController, IndexingModel, PackageStore, AdminRoute, SearchRoute, ViewPackageRoute, Footer, PackageIcon) {

    var app = em.Application.create({name: "NuGet"});

    app.indexingModel = IndexingModel.create();
    app.Packages = PackageStore.create();

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