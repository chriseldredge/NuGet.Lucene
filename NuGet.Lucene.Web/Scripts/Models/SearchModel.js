define(['ember', 'Models/PaginationSupport'], function (em, PaginationSupport) {
    return em.Object.extend(PaginationSupport, {
        data: {},
        query: '',
        loaded: false,
        totalBinding: 'data.totalHits',
        hits: function () {
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
        }.property('data.hits').cacheable(),

        search: function (query) {
            console.log('set query to ' + query);
            this.setProperties({ loaded: false, page: 0, query: query });
            this.load();
        },
        
        didRequestPage: function () {
            console.log('page changed to ' + this.get('page'));
            this.load();
        },
        
        load: function () {
            var self = this;

            console.log('load search results for query ' + this.get('query') + ' page ' + this.get('page'));

            $.ajax("api/v2/package", {
                type: 'GET',
                data: {
                    query: self.get('query'),
                    offset: self.get('page') * self.get('pageSize'),
                    count: self.get('pageSize')
                },
                success: function (data, status, xhr) {
                    self.set('data', data);
                    self.set('loaded', true);
                }
            });
        },
    });
});