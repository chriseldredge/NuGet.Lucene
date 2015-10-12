namespace NuGet.Lucene.Web
{
    public static class RouteNames
    {
        public const string Indexing = "Indexing";

        public static class Users
        {
            public const string All = "Users.GetAll";
            public const string GetUser = "Users.GetUser";
            public const string PutUser = "Users.PutUser";
            public const string PostUser = "Users.PostUser";
            public const string DeleteAll = "Users.DeleteAll";
            public const string DeleteUser = "Users.DeleteUser";
            public const string GetAuthenticationInfo = "Users.GetAuthenticationInfo";
            public const string GetRequiredAuthenticationInfo = "Users.GetRequiredAuthenticationInfo";
            public const string ChangeApiKey = "Users.ChangeApiKey";
        }

        public static class Packages
        {
            public const string Search = "Packages.Search";
            public const string GetAvailableSearchFieldNames = "Packages.GetAvailableSearchFieldNames";
            public const string Upload = "Packages.Upload";
            public const string Delete = "Packages.Delete";
            public const string Info = "Packages.Info";
            public const string Download = "Packages.Download";
            public const string DownloadLatestVersion = "Packages.Download.Latest";
            public const string Feed = "OData Package Feed";
        }

        public static class Redirect
        {
            public const string RootFeed = "NuGetClient.Redirect.RootFeed";
            public const string ApiFeed = "NuGetClient.Redirect.ApiFeed";
            public const string Upload = "NuGetClient.Redirect.Upload";
            public const string Delete = "NuGetClient.Redirect.Delete";
        }

        public const string Sources = "Sources";

        public static class Symbols
        {
            public const string GetFile = "Symbols.GetFile";
            public const string Settings = "Symbols.Settings";
            public const string Upload = "Symbols.Upload";
        }

        public static class TabCompletion
        {
            public const string VS2013PackageIds = "Package Manager Console Tab Completion - Package IDs - VS2013";
            public const string VS2013PackageVersions = "Package Manager Console Tab Completion - Package Versions - VS 2013";

            public const string VS2015PackageIds = "Package Manager Console Tab Completion - Package IDs - VS2015";
            public const string VS2015PackageVersions = "Package Manager Console Tab Completion - Package Versions - VS2015";
        }
    }
}
