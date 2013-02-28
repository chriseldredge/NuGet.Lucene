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
    this.route('admin');
});

App.AdminRoute = Ember.Route.extend({
    model: function () {
        return App.indexingModel;
    }
});

$(function () {
    App.indexingModel = App.IndexingModel.create();
    App.advanceReadiness();
});
