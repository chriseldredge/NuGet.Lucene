using System.Web.Routing;

namespace NuGet.Lucene.Web
{
    public static class RouteNames
    {
        public const string PackageFeed = "OData Package Feed";
        public static readonly RouteValueDictionary PackageFeedRouteValues = new RouteValueDictionary { { "serviceType", "odata" } };

        public const string Home = "Home";
        public const string IndexingStatus = "Status";

        public const string UserApi = "UserApi";
        public const string PackageApi = "PackageApi";

        public const string PackageInfo = "Package Info";
        public const string PackageDownload = "Download Package";
        public const string PackageDownloadAnyVersion = "Package Download - Latest Version";
        
        public const string TabCompletionPackageIds = "Package Manager Console Tab Completion - Package IDs";
        public const string TabCompletionPackageVersions = "Package Manager Console Tab Completion - Package Versions";
    }
}