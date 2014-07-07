using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Routing;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web
{
    public class ODataRedirectHandler : RedirectHandler
    {
        public ODataRedirectHandler(string routeName) : base(routeName)
        {
        }

        protected override string GetRedirectLink(HttpRequestMessage request)
        {
            return new UrlHelper(request).CreateODataLink(routeName, request.ODataProperties().PathHandler, new List<ODataPathSegment>());
        }
    }
}