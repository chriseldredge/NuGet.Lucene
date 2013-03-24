var BaseDataUrl = '';
var ApiKey = '';

require.config({
    paths: {
        'handlebars': 'lib/handlebars-1.0.9',
        'ember': 'lib/ember-1.0.0-rc.1',
        'signalr': 'lib/jquery.signalR-1.0.1',
        'signalr.hubs': BaseDataUrl + '../signalr/hubs?noext'
    },
    shim: {
        'ember': {
            deps: ['handlebars'],
            exports: 'Ember'
        },
        'signalr.hubs': {
            deps: ['signalr'],
        }
    }
});

require(["app", "Router"], function (app) {
    window.App = app;
});