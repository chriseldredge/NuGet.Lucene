using System.Web.Http.OData.Formatter.Serialization;
using Microsoft.Data.Edm;

namespace NuGet.Lucene.Web.OData.Formatter.Serialization
{
    public class ODataPackageDefaultStreamAwareSerializerProvider : DefaultODataSerializerProvider
    {
        private readonly ODataEdmTypeSerializer entitySerializer;

        public ODataPackageDefaultStreamAwareSerializerProvider()
        {
            this.entitySerializer = new ODataPackageDefaultStreamAwareEntityTypeSerializer(this);
        }

        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.IsEntity())
            {
                return entitySerializer;
            }
            
            return base.GetEdmTypeSerializer(edmType);
        }
    }
}