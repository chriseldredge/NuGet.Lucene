using System;
using System.Net.Http;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web
{
    public class DeletePackageRedirectHandler : RedirectHandler
    {
        public DeletePackageRedirectHandler()
            : base(RouteNames.Packages.Delete)
        {
        }

        protected override Uri GetRedirectUri(HttpRequestMessage request)
        {
            var url = new UrlHelper(request).Link(routeName, request.GetRouteData().Values);
            return new Uri(url);
        }
    }
}