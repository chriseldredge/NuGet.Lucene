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

App.Search = Ember.Object.extend({
    data: {},
    query: '',
    pageSize: 10,
    loaded: false,
    pageBinding: 'data.page',
    totalHitsBinding: 'data.totalHits',
    firstBinding: 'data.first',
    lastBinding: 'data.last',
    isFirstPage: function() {
        return this.get('page') === 0;
    }.property('page'),
    isLastPage: function() {
        return this.get('totalHits') <= this.get('last');
    }.property('last'),
    hits: function() {
        var copy = [];
        var hits = this.get('data.hits');
        if (!hits) return copy;
        for (var i = 0; i < hits.length; i++) {
            var hit = hits[i];
            var c = {};
            for (var attr in hit) {
                c[attr] = hit[attr];
            }
            var tagQueries = [];
            if (!hit.tags) hit.tags = '';
            var tags = hit.tags.split(' ');
            c.tags = [];
            for (var j = 0; j < tags.length; j++) {
                if (tags[j] === '') continue;
                tagQueries.push(tags[j]);
            }
            c.tags = tagQueries;
            copy.push(c);
        }
        return copy;
    }.property('data.hits'),
    load: function(query, page) {
        var self = this;
        console.log('loading search results');
        
        $.ajax("api/v2/package", {
            type: 'GET',
            data: {query: query, page: page, pageSize: self.get('pageSize')},
            success: function(data, status, xhr) {
                self.set('data', data);
                self.set('loaded', true);
            }
        });
    }
});

App.ApplicationController = Ember.Controller.extend({
    needs: 'search',
    queryBinding: Ember.Binding.oneWay('controllers.search.query'),
    search: function () {
        this.transitionToRoute('search', this.get('query'));
    }
});

App.SearchController = Ember.ObjectController.extend({
    content: App.Search.create(),
    query: '',
    search: function (query) {
        console.log('searchController.search ' + query);
        console.log('searchController model ' + this.get('model'));
        this.set('query', query);   
        this.get('model').load(query);
    },
    nextPage: function() {
        this.get('model').load(this.get('query'), this.get('page') + 1);
    },
    previousPage: function() {
        this.get('model').load(this.get('query', this.get('page') - 1));
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

        controller.set('model', App.Search.create());
        controller.search(query);
    }
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
