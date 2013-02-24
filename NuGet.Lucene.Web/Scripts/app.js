App = Ember.Application.create({
    name: "NuGet",
    LOG_TRANSITIONS: true,
});

App.packagesController = Ember.Object.create({
    status: undefined,
    getStatus: function () {
        $.getJSON('/api/status', function (data) {
            App.packagesController.set('status', data);
        });
    }
});

App.StatusView = Em.View.extend({
    templateName: 'indexing-status',
    statusBinding: 'App.packagesController.status',
});

App.packagesController.getStatus();
