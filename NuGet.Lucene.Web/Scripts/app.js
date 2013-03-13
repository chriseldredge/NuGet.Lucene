define([
        'ember',
        'Models/IndexingModel',
        'Models/SearchModel'
], function (em, IndexingModel, SearchModel) {

    var app = em.Application.create({
        name: "NuGet",
    });

    app.deferReadiness();

    app.IndexingModel = IndexingModel;
    app.Search = SearchModel;

        app.ApplicationController = em.Controller.extend({
            needs: 'search',
            queryBinding: em.Binding.oneWay('controllers.search.query'),
            search: function() {
                this.transitionToRoute('search', this.get('query'));
            }
        });

        app.SearchController = em.ObjectController.extend({
            query: '',
            search: function (query) {
                this.set('query', query);
                this.get('model').search(this.get('query'));
            },
            nextPage: function() {
                this.get('model').nextPage();
            },
            previousPage: function() {
                this.get('model').previousPage();
            }
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
            setupController: function(controller, context) {
                controller.set('model', app.Search.create());
                
                if (typeof context === "string") {
                    controller.search(context);
                } else {
                    controller.search(context.query);
                }
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

        $(document).ready(function() {
            app.indexingModel = app.IndexingModel.create();
            app.advanceReadiness();
        });

        $(document).ready(function() {
            $(".package-icon").error(function() {
                $(this).unbind("error").attr("src", "http://nuget.org/Content/Images/packageDefaultIcon-50x50.png");
            });
        });

        return app;
    });