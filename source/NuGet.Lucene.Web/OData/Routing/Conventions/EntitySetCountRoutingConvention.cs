using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace NuGet.Lucene.Web.OData.Routing.Conventions
{
    /// <summary>
    /// Adds support for $count operation on entity sets when controller has a <c>GetCount</c> action.
    /// </summary>
    public class EntitySetCountRoutingConvention : EntitySetRoutingConvention
    {
        public override string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (controllerContext.Request.Method != HttpMethod.Get || odataPath.PathTemplate != "~/entityset/$count")
            {
                return null;
            }

            return actionMap.Contains("GetCount") ? "GetCount" : null;
        }
    }
}