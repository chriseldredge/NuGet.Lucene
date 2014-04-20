using System.Web.Http.OData;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.DataServices
{
    public class ODataPackageNamedStreamAwareEntityTypeSerializer : NamedStreamAwareEntityTypeSerializer
    {
        public ODataPackageNamedStreamAwareEntityTypeSerializer(ODataSerializerProvider serializerProvider) : base(serializerProvider)
        {
        }

        protected override string BuildLinkForStreamProperty(EntityInstanceContext entity, IEdmStructuralProperty streamProperty)
        {
            var url = new UrlHelper(entity.Request);
            var package = (ODataPackage)entity.EntityInstance;
            var routeParams = new { package.Id, package.Version };
            return url.Link(RouteNames.Packages.Download, routeParams);
        }

        protected override string ContentType
        {
            get { return "application/zip"; }
        }
    }
}