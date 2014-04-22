using System.Web.Http.OData.Routing;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace NuGet.Lucene.Web.OData.Routing
{
    public class CountPathSegment : ODataPathSegment
    {
        public override string SegmentKind
        {
            get
            {
                return "$count";
            }
        }

        public override IEdmType GetEdmType(IEdmType previousEdmType)
        {
            return EdmCoreModel.Instance.FindDeclaredType("Edm.Int32");
        }

        public override IEdmEntitySet GetEntitySet(IEdmEntitySet previousEntitySet)
        {
            return previousEntitySet;
        }

        public override string ToString()
        {
            return "$count";
        }
    }
}