using System;
using System.Web.Http.Routing;

namespace NuGet.Lucene.Web.Util
{
    public static class UrlHelperExtensions
    {
        public static string GetSymbolSourceUri(this UrlHelper url)
        {
            var uri = url.RequiredLink(RouteNames.Sources, new {id = "DUMMY", version = "1.0", path = ""});

            // Remove path components to get root URI
            return uri.Substring(0, uri.IndexOf("/DUMMY", StringComparison.Ordinal));
        }

        public static string GetSymbolsUri(this UrlHelper url)
        {
            var uri = url.RequiredLink(RouteNames.Symbols.GetFile, new { path = "DUMMY" });

            // Remove path components to get root URI
            return uri.Substring(0, uri.IndexOf("/DUMMY", StringComparison.Ordinal));
        }

        public static string RequiredLink(this UrlHelper url, string routeName, object routeValues = null)
        {
            var link = url.Link(routeName, routeValues);

            if (string.IsNullOrEmpty(link))
            {
                throw new InvalidOperationException("Failed to resolve URI for route name " + routeName + " with provided route values.");
            }

            return link;
        }

    }
}
