using System.Web.Http.OData;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.Routing;
using NuGet.Lucene.Web.Models;
using System;

namespace NuGet.Lucene.Web.OData.Formatter.Serialization
{
    public class ODataPackageDefaultStreamAwareEntityTypeSerializer : DefaultStreamAwareEntityTypeSerializer<ODataPackage>
    {
        public ODataPackageDefaultStreamAwareEntityTypeSerializer(ODataSerializerProvider serializerProvider) : base(serializerProvider)
        {
        }

        public override Uri BuildLinkForStreamProperty(ODataPackage package, EntityInstanceContext context)
        {
            var url = new UrlHelper(context.Request);
            var routeParams = new { package.Id, package.Version };
            return new Uri(url.Link(RouteNames.Packages.Download, routeParams), UriKind.Absolute);
        }

        public override string ContentType
        {
            get { return "application/zip"; }
        }
    }
}