using System;
using System.Web.Http.OData;
using System.Web.Http.OData.Formatter.Serialization;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;

namespace NuGet.Lucene.Web.DataServices
{
    public abstract class NamedStreamAwareEntityTypeSerializer : ODataEntityTypeSerializer
    {
        protected NamedStreamAwareEntityTypeSerializer(ODataSerializerProvider serializerProvider)
            : base(serializerProvider)
        {
        }

        public override ODataProperty CreateStructuralProperty(IEdmStructuralProperty structuralProperty,
            EntityInstanceContext entityInstanceContext)
        {
            if (structuralProperty.Type.IsStream())
            {
                return ToNamedStreamProperty(entityInstanceContext, structuralProperty);
            }

            return base.CreateStructuralProperty(structuralProperty, entityInstanceContext);
        }

        private ODataProperty ToNamedStreamProperty(EntityInstanceContext entity,
            IEdmStructuralProperty streamProperty)
        {
            var href = BuildLinkForStreamProperty(entity, streamProperty);

            return new ODataProperty
            {
                Name = streamProperty.Name,
                Value = new ODataStreamReferenceValue
                {
                    ContentType = ContentType,
                    ReadLink = new Uri(href)
                }
            };
        }

        protected virtual string ContentType
        {
            get { return "application/octet-stream"; }
        }

        protected abstract string BuildLinkForStreamProperty(EntityInstanceContext entity, IEdmStructuralProperty streamProperty);
    }
}
