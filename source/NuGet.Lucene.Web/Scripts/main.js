require.config({
    paths: {
        'handlebars': 'lib/handlebars-1.0.9',
        'ember': 'lib/ember-1.0.0-rc.1',
        'signalR': 'lib/jquery.signalR-1.0.1'
    },
    shim: {
        'ember': {
            deps: ['handlebars'],
            exports: 'Ember'
        },
        'signalR': {
            deps: ['jquery'],
            init: function ($) {
                return $.signalR;
            }
        }
    }
});

require(['app', 'Router'], function (app) {
    window.App = app;
});