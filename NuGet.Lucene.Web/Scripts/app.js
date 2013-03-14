define([
        'ember',
        'Models/IndexingModel',
        'Models/Packages',
        'Models/PaginationSupport'
], function (em, IndexingModel, Packages, PaginationSupport) {

    var app = em.Application.create({
        name: "NuGet",
    });

    app.IndexingModel = IndexingModel;
    app.Packages = Packages.create();
    
        app.ApplicationController = em.Controller.extend({
            needs: 'search',
            queryBinding: em.Binding.oneWay('controllers.search.query'),
            search: function() {
                this.get('controllers.search').goTo(this.get('query'));
            }
        });

        app.SearchController = em.ObjectController.extend(PaginationSupport, {
            totalBinding: em.Binding.oneWay('model.totalHits'),
            pageBinding: em.Binding.oneWay('model.page'),
            goTo: function (query) {
                var model = app.Packages.search(query, 0, this.get('pageSize'));
                this.transitionToRoute('search', model);
            },
            didRequestPage: function () {
                // when a new search is being loaded, ignore when the page gets set back to zero.
                if (this.get('loading')) return;
                
                var model = app.Packages.search(this.get('query'), this.get('page'), this.get('pageSize'));
                this.set('model', model);
            },
        });

        app.AdminController = em.ObjectController.extend({
            synchronize: function() {
                app.indexingModel.synchronize();
            },
            cancel: function() {
                app.indexingModel.cancel();
            }
        });

        app.FooterView = em.View.extend({
            templateName: 'footer',
            tagName: 'footer',
            contentBinding: 'App.indexingModel.status'
        });

    app.PackageIcon = em.View.extend({
        DefaultIconUrl: 'img/package-default-icon-50x50.png',
        tagName: 'img',
        classNames: ['package-icon'],
        attributeBindings: ['src', 'alt'],
        alt: 'package icon',
        src: function () {
            var url = this.get('content.iconUrl');

            if (!url) {
                url = this.DefaultIconUrl;
            }
            return url;
            
        }.property('content.iconUrl'),
        didInsertElement: function () {
            var self = this;
            var img = $(this.get('element'));
            img.error(function () {
                img.unbind("error").attr("src", self.DefaultIconUrl);
            });
        }
    });
    
        app.Router.map(function() {
            this.route('index', { path: '/' });
            this.route('admin');
            this.route('search', { path: '/package/search/:query' });
            this.route('viewPackage', { path: '/package/:id/:version' });
        });

        app.AdminRoute = em.Route.extend({
            model: function() {
                return app.indexingModel;
            }
        });

        app.SearchRoute = em.Route.extend({
            model: function (params) {
                return app.Packages.search(params.query, 0, 10);
            },
            serialize: function(model) {
                return { query: model.query };
            }
        });

        app.ViewPackageRoute = em.Route.extend({
            setupController: function(controller, params) {
                controller.set('id', params.id);
                controller.set('version', params.version);
            },
            serialize: function(model) {
                return { id: model.id, version: model.version };
            },
        });

        app.indexingModel = app.IndexingModel.create();

        return app;
    });