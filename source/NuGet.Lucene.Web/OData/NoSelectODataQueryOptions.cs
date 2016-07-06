using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.OData;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;
using Microsoft.Data.OData.Query;

namespace NuGet.Lucene.Web.OData
{
    public class NoSelectODataQueryOptions<T> : ODataQueryOptions<T>
    {
        private static readonly MethodInfo limitResultsGenericMethod = typeof(ODataQueryOptions).GetMethod("LimitResults");
        private readonly IAssembliesResolver assembliesResolver;

        public NoSelectODataQueryOptions(ODataQueryContext context, HttpRequestMessage request) : base(context, request)
        {
            if (request.GetConfiguration() != null)
            {
                assembliesResolver = request.GetConfiguration().Services.GetAssembliesResolver();
            }

            // fallback to the default assemblies resolver if none available.
            assembliesResolver = assembliesResolver ?? new DefaultAssembliesResolver();

        }
        public override IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (querySettings == null)
            {
                throw new ArgumentNullException(nameof(querySettings));
            }

            var result = query;

            // Construct the actual query and apply them in the following order: filter, orderby, skip, top
            if (Filter != null)
            {
                result = Filter.ApplyTo(result, querySettings, assembliesResolver);
            }

            if (InlineCount != null && Request.ODataProperties().TotalCount == null)
            {
                long? count = InlineCount.GetEntityCount(result);
                if (count.HasValue)
                {
                    Request.ODataProperties().TotalCount = count.Value;
                }
            }

            OrderByQueryOption orderBy = OrderBy;

            // $skip or $top require a stable sort for predictable results.
            // Result limits require a stable sort to be able to generate a next page link.
            // If either is present in the query and we have permission,
            // generate an $orderby that will produce a stable sort.
            if (querySettings.EnsureStableOrdering &&
                (Skip != null || Top != null || querySettings.PageSize.HasValue))
            {
                // If there is no OrderBy present, we manufacture a default.
                // If an OrderBy is already present, we add any missing
                // properties necessary to make a stable sort.
                // Instead of failing early here if we cannot generate the OrderBy,
                // let the IQueryable backend fail (if it has to).
                orderBy = orderBy == null
                            ? GenerateDefaultOrderBy(Context)
                            : EnsureStableSortOrderBy(orderBy, Context);
            }

            if (orderBy != null)
            {
                result = orderBy.ApplyTo(result, querySettings);
            }

            if (Skip != null)
            {
                result = Skip.ApplyTo(result, querySettings);
            }

            if (Top != null)
            {
                result = Top.ApplyTo(result, querySettings);
            }

            if (querySettings.PageSize.HasValue)
            {
                bool resultsLimited;
                result = LimitResults(result, querySettings.PageSize.Value, out resultsLimited);
                if (resultsLimited && Request.RequestUri != null && Request.RequestUri.IsAbsoluteUri && Request.ODataProperties().NextLink == null)
                {
                    Uri nextPageLink = GetNextPageLink(Request, querySettings.PageSize.Value);
                    Request.ODataProperties().NextLink = nextPageLink;
                }
            }

            return result;
        }
        private static OrderByQueryOption EnsureStableSortOrderBy(OrderByQueryOption orderBy, ODataQueryContext context)
        {
            Contract.Assert(orderBy != null);
            Contract.Assert(context != null);

            // Strategy: create a hash of all properties already used in the given OrderBy
            // and remove them from the list of properties we need to add to make the sort stable.
            var usedPropertyNames =
                new HashSet<string>(orderBy.OrderByNodes.OfType<OrderByPropertyNode>().Select(node => node.Property.Name));

            var propertiesToAdd = GetAvailableOrderByProperties(context).Where(prop => !usedPropertyNames.Contains(prop.Name)).ToArray();

            if (propertiesToAdd.Any())
            {
                // The existing query options has too few properties to create a stable sort.
                // Clone the given one and add the remaining properties to end, thereby making
                // the sort stable but preserving the user's original intent for the major
                // sort order.
                orderBy = new OrderByQueryOption(orderBy.RawValue, context);
                foreach (var property in propertiesToAdd)
                {
                    orderBy.OrderByNodes.Add(new OrderByPropertyNode(property, OrderByDirection.Ascending));
                }
            }

            return orderBy;
        }

        private static OrderByQueryOption GenerateDefaultOrderBy(ODataQueryContext context)
        {
            var orderByRaw = string.Join(",",
                                    GetAvailableOrderByProperties(context)
                                        .Select(property => property.Name));
            return string.IsNullOrEmpty(orderByRaw)
                    ? null
                    : new OrderByQueryOption(orderByRaw, context);
        }

        // Returns a sorted list of all properties that may legally appear
        // in an OrderBy.  If the entity type has keys, all are returned.
        // Otherwise, when no keys are present, all primitive properties are returned.
        private static IEnumerable<IEdmStructuralProperty> GetAvailableOrderByProperties(ODataQueryContext context)
        {
            Contract.Assert(context != null);

            var entityType = context.ElementType as IEdmEntityType;

            if (entityType == null) return Enumerable.Empty<IEdmStructuralProperty>();

            var properties =
                entityType.Key().Any()
                    ? entityType.Key()
                    : entityType
                        .StructuralProperties()
                        .Where(property => property.Type.IsPrimitive());

            // Sort properties alphabetically for stable sort
            return properties.OrderBy(property => property.Name);
        }
        internal static IQueryable LimitResults(IQueryable queryable, int limit, out bool resultsLimited)
        {
            var genericMethod = limitResultsGenericMethod.MakeGenericMethod(queryable.ElementType);
            var args = new object[] { queryable, limit, null };
            var results = genericMethod.Invoke(null, args) as IQueryable;
            resultsLimited = (bool)args[2];
            return results;
        }

        internal static Uri GetNextPageLink(HttpRequestMessage request, int pageSize)
        {
            Contract.Assert(request != null);
            Contract.Assert(request.RequestUri != null);
            Contract.Assert(request.RequestUri.IsAbsoluteUri);

            return GetNextPageLink(request.RequestUri, request.GetQueryNameValuePairs(), pageSize);
        }

        internal static Uri GetNextPageLink(Uri requestUri, int pageSize)
        {
            Contract.Assert(requestUri != null);
            Contract.Assert(requestUri.IsAbsoluteUri);

            return GetNextPageLink(requestUri, new FormDataCollection(requestUri), pageSize);
        }

        internal static Uri GetNextPageLink(Uri requestUri, IEnumerable<KeyValuePair<string, string>> queryParameters, int pageSize)
        {
            Contract.Assert(requestUri != null);
            Contract.Assert(queryParameters != null);
            Contract.Assert(requestUri.IsAbsoluteUri);

            var queryBuilder = new StringBuilder();

            var nextPageSkip = pageSize;

            foreach (KeyValuePair<string, string> kvp in queryParameters)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                switch (key)
                {
                    case "$top":
                        int top;
                        if (int.TryParse(value, out top))
                        {
                            // There is no next page if the $top query option's value is less than or equal to the page size.
                            Contract.Assert(top > pageSize);
                            // We decrease top by the pageSize because that's the number of results we're returning in the current page
                            value = (top - pageSize).ToString(CultureInfo.InvariantCulture);
                        }
                        break;
                    case "$skip":
                        int skip;
                        if (int.TryParse(value, out skip))
                        {
                            // We increase skip by the pageSize because that's the number of results we're returning in the current page
                            nextPageSkip += skip;
                        }
                        continue;
                }

                if (key.Length > 0 && key[0] == '$')
                {
                    // $ is a legal first character in query keys
                    key = '$' + Uri.EscapeDataString(key.Substring(1));
                }
                else
                {
                    key = Uri.EscapeDataString(key);
                }
                value = Uri.EscapeDataString(value);

                queryBuilder.Append(key);
                queryBuilder.Append('=');
                queryBuilder.Append(value);
                queryBuilder.Append('&');
            }

            queryBuilder.AppendFormat("$skip={0}", nextPageSkip);

            var uriBuilder = new UriBuilder(requestUri)
            {
                Query = queryBuilder.ToString()
            };
            return uriBuilder.Uri;
        }
    }
}
