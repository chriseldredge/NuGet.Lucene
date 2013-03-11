App = Ember.Application.create({
    name: "NuGet",
});

App.deferReadiness();

App.IndexingModel = Ember.Object.extend({
    status: {},
    hub: undefined,
    init: function() {
        console.log("Connecting to SignalR status hub");
        var self = this;
        var setStatusCallback = function(status) {
            self.set('status', status);
        };
        
        $.connection.hub.logging = true;

        hub = $.connection.status;

        hub.client.updateStatus = setStatusCallback;

        hub.connection.stateChanged(function (change) {
            var isConnected = change.newState === $.signalR.connectionState.connected;
            self.set('isConnected', isConnected);
            
            if (isConnected) {
                hub.server.getStatus().done(setStatusCallback);
            } else {
                setStatusCallback({});
            }
        });

        this.set('hub', hub);

        $.connection.hub.start();
    },
    synchronize: function() {
        $.ajax("api/indexing/synchronize", { type: 'POST' });
    },
    cancel: function() {
        $.ajax("api/indexing/cancel", { type: 'POST' });
    },
    isRunning: function() {
        return this.status.synchronizationState != 'Idle';
    }.property('status'),
});

App.ApplicationController = Ember.Controller.extend({
    needs: 'search',
    queryBinding: 'controllers.search.query',
    search: function () {
        this.transitionToRoute('search', this.get('query'));
    }
});

App.SearchController = Ember.ObjectController.extend({
    content: {},
    query: '',
    page: 0,
    pageSize: 20,
    search: function(query) {
        this.set('query', query);
        this.fetch(query);
    },
    fetch: function(query) {
        var self = this;
        $.ajax("api/v2/package", {
            type: 'GET',
            data: {query: query},
            success: function(data, status, xhr) {
                self.set('content', data);
            }
        });
    }
});

App.AdminController = Ember.ObjectController.extend({
    synchronize: function () {
        App.indexingModel.synchronize();
    },
    cancel: function () {
        App.indexingModel.cancel();
    }
});

App.FooterView = Ember.View.extend({
    templateName: 'footer',
    tagName: 'footer',
    contentBinding: 'App.indexingModel.status'
});

App.Router.map(function () {
    this.route('index', { path: '/' });
    this.route('admin');
    this.route('search', { path: '/search/:query'});
});

App.AdminRoute = Ember.Route.extend({
    model: function () {
        return App.indexingModel;
    }
});

App.SearchRoute = Ember.Route.extend({
    setupController: function(controller, params) {
        var query = params;

        if (params && params.query) {
            console.log("search for complex " + query.query);
            query = params.query;
        }
        else
        {
            console.log("search for string " + query);   
        }

        controller.search(query);
    }
});

Ember.Handlebars.registerBoundHelper('split', function(value, options) {
    if (!value) value = '';
    var items = value.split(' ');
    var result = "";

    for (var i=0; i<items.length; i++) {
        if (items[i] !== '') {
            var item = { tag: items[i] };
            result += options.fn(item);
        }
    }

    return result;
});

$(function () {
    App.indexingModel = App.IndexingModel.create();
    App.advanceReadiness();
});

$(document).ready(function() {
    $(".package-icon").error(function() {
        $(this).unbind("error").attr("src", "http://nuget.org/Content/Images/packageDefaultIcon-50x50.png");
    });
});
