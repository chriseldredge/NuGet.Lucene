define(['ember', 'signalr', 'signalr.hubs'], function (em) {
    return em.Object.extend({
        restClient: null,
        status: {},
        hub: undefined,
        init: function () {
            console.log("Connecting to SignalR status hub " + $.connection.version);
            var self = this;
            var setStatusCallback = function (status) {
                self.set('status', status);
            };

            $.connection.hub.logging = true;

            hub = $.connection.status;

            hub.client.updateStatus = setStatusCallback;

            hub.connection.stateChanged(function (change) {
                var isConnected = change.newState === $.connection.connectionState.connected;
                self.set('isConnected', isConnected);

                if (isConnected) {
                    hub.server.getStatus().done(setStatusCallback);
                } else {
                    setStatusCallback({});
                }
            });

            this.set('hub', hub);

            $.connection.hub.url = BaseDataUrl + '../signalr';
            
            $.connection.hub.start({ waitForPageLoad: false });
        },
        synchronize: function () {
            this.get('restClient').ajax('indexing.synchronize', {
                type: 'POST',
            });
        },
        cancel: function () {
            this.get('restClient').ajax('indexing.cancel', {
                type: 'POST',
            });
        },
        isRunning: function () {
            return this.status.synchronizationState != 'Idle';
        }.property('status'),
    });
});