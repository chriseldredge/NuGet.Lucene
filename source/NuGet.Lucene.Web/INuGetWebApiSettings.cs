using System;
using System.Collections.Specialized;

namespace NuGet.Lucene.Web
{
    public interface INuGetWebApiSettings
    {
        // Security
        bool ShowExceptionDetails { get; }
        
        bool EnableCrossDomainRequests { get; }
        
        bool HandleLocalRequestsAsAdmin { get; }
        string LocalAdministratorApiKey { get; }

        bool AllowAnonymousPackageChanges { get; }
        
        bool RoleMappingsEnabled { get; }
        NameValueCollection RoleMappings { get; }

        // Web
        string RoutePathPrefix { get; }

        // Mirroring
        string PackageMirrorTargetUrl { get; }
        bool AlwaysCheckMirror { get; }
        TimeSpan PackageMirrorTimeout { get; }

        // Paths
        string PackagesPath { get; }
        bool GroupPackageFilesById { get; }
        string LucenePackagesIndexPath { get; }

        string LuceneUsersIndexPath { get; }

        string SymbolsPath { get; }
        string ToolsPath { get; }

        // Options
        bool SynchronizeOnStart { get; }
        bool EnablePackageFileWatcher { get; }
        bool KeepSourcesCompressed { get; }
    }
}
