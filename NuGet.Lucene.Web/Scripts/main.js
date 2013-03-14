require.config({
    paths: {
        'handlebars': 'lib/handlebars-1.0.9',
        'ember': 'lib/ember-1.0.0-rc.1',
        'signalr': 'lib/jquery.signalR-1.0.1',
        'signalr.hubs': '../signalr/hubs?noext'
    },
    shim: {
        'ember': {
            deps: ['handlebars'],
            exports: 'Ember'
        },
        'signalr': {
            exports: '$.connection'
        },
        'signalr.hubs': {
            deps: ['signalr'],
            exports: '$.connection'
        }
    }
});

require(["app", "Router"], function (app) {
    window.App = app;
});