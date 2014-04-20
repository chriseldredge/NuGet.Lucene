using System.Web.Http.OData.Formatter.Serialization;
using Microsoft.Data.Edm;

namespace NuGet.Lucene.Web.DataServices
{
    public class NamedStreamAwareSerializerProvider : DefaultODataSerializerProvider
    {
        private readonly NamedStreamAwareEntityTypeSerializer customEntitySerializer;
        
        public NamedStreamAwareSerializerProvider()
        {
            customEntitySerializer = new ODataPackageNamedStreamAwareEntityTypeSerializer(this);
        }

        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.IsEntity())
            {
                return customEntitySerializer;
            }

            return base.GetEdmTypeSerializer(edmType);
        }
    }
}