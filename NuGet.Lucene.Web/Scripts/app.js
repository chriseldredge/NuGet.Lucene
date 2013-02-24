App = Ember.Application.create({
    name: "NuGet",
    indexingStatus: undefined,
});

App.StatusView = Em.View.extend({
    templateName: 'indexing-status',
    statusBinding: 'App.indexingStatus',
});

$(function () {
    var hub = $.connection.status;

    hub.client.updateStatus = function (status) {
        App.set('indexingStatus', status);
    };

    $.connection.hub.start()
        .done(function() {
            hub.server.getStatus()
                .done(function(status) { App.set('indexingStatus', status); });
        });
});
