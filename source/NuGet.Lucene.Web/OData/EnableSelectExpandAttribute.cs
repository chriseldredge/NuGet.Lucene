using System.Linq;
using System.Web.Http.OData;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Query;

namespace NuGet.Lucene.Web.OData
{
    public class EnableSelectExpandAttribute : EnableQueryAttribute
    {
        public override IQueryable ApplyQuery(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            if (queryOptions.SelectExpand == null) return queryable;

            queryOptions.Request.ODataProperties().SelectExpandClause = queryOptions.SelectExpand.SelectExpandClause;

            return queryOptions.SelectExpand.ApplyTo(queryable, new ODataQuerySettings());
        }
    }
}
