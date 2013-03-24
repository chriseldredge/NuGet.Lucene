define(['ember'], function (em) {
    return em.Object.extend({
        baseUrl: '',
        apiKey: '',
        apiInfo: [],
        ready: Ember.K,
        init: function () {
            var self = this;
            $.ajax(this.get('baseUrl'), {
                type: 'GET',
                success: function (data) {
                    var apiInfo = {};
                    for (var i=0; i<data.length; i++) {
                        var name = data[i]['name'].toLowerCase();
                        var key = data[i]['method'] + '.' + name;

                        if (key in apiInfo) {
                            console.warn('Duplicate api method: ' + key);
                        }
                        apiInfo[key] = data[i];
                    }
                    self.set('apiInfo', apiInfo);
                    self.get('ready')();
                }
            });
        },
        ajax: function (apiName, options) {
            var method = 'GET';
            if ('type' in options) {
                method = options.type;
            }
            var key = method + '.' + apiName.toLowerCase();
            var api = this.get('apiInfo')[key];
            
            if (!api) {
                throw 'Rest API method not found: ' + apiName;
            }
            
            options.type = api.method;

            if (this.get('apiKey') !== '') {
                var origBeforeSend = options['beforeSend'];

                options.beforeSend = function(xhr) {
                    xhr.setRequestHeader('X-NuGet-ApiKey', 'example');
                    if (origBeforeSend) {
                        origBeforeSend(xhr);
                    }
                };
            }
            
            $.ajax(api.href, options);
        }
    });
});