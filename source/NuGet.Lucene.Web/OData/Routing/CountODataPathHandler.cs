using System.Web.Http.OData.Routing;
using Microsoft.Data.Edm;

namespace NuGet.Lucene.Web.OData.Routing
{
    public class CountODataPathHandler : DefaultODataPathHandler
    {
        protected override ODataPathSegment ParseAtEntityCollection(IEdmModel model, ODataPathSegment previous, IEdmType previousEdmType, string segment)
        {
            if (segment == "$count")
            {
                return new CountPathSegment();
            }
            return base.ParseAtEntityCollection(model, previous, previousEdmType, segment);
        }
    }
}
