using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiSettings : INuGetWebApiSettings
    {
        public const string DefaultAppSettingPrefix = "NuGet.Lucene.Web:";
        public const string BlankAppSettingPrefix = "";

        public const string DefaultRoutePathPrefix = "api/";

        private readonly string prefix;
        private readonly NameValueCollection settings;
        private readonly NameValueCollection roleMappings;

        public NuGetWebApiSettings()
            : this(DefaultAppSettingPrefix)
        {
        }

        public NuGetWebApiSettings(string prefix)
            : this(prefix, ConfigurationManager.AppSettings, ConfigurationManager.GetSection("roleMappings") as NameValueCollection)
        {
        }

        public NuGetWebApiSettings(string prefix, NameValueCollection settings, NameValueCollection roleMappings)
        {
            this.prefix = prefix;
            this.settings = settings;
            this.roleMappings = roleMappings ?? new NameValueCollection();
        }

        public bool ShowExceptionDetails
        {
            get { return GetFlagFromAppSetting("showExceptionDetails", false); }
        }

        public bool EnableCrossDomainRequests
        {
            get { return GetFlagFromAppSetting("enableCrossDomainRequests", false); }
        }

        public bool HandleLocalRequestsAsAdmin
        {
            get { return GetFlagFromAppSetting("handleLocalRequestsAsAdmin", false); }
        }

        public string LocalAdministratorApiKey
        {
            get
            {
                return GetAppSetting("localAdministratorApiKey", string.Empty);
            }
        }

        public bool AllowAnonymousPackageChanges
        {
            get { return GetFlagFromAppSetting("allowAnonymousPackageChanges", false); }
        }

        public string RoutePathPrefix
        {
            get { return GetAppSetting("routePathPrefix", DefaultRoutePathPrefix); }
        }

        public string PackageMirrorTargetUrl
        {
            get { return GetAppSetting("packageMirrorTargetUrl", String.Empty); }
        }

        public bool AlwaysCheckMirror
        {
            get { return GetFlagFromAppSetting("alwaysCheckMirror", false); }
        }

        public TimeSpan PackageMirrorTimeout
        {
            get
            {
                var str = GetAppSetting("packageMirrorTimeout", "0:00:15");
                TimeSpan ts;
                return TimeSpan.TryParse(str, out ts) ? ts : TimeSpan.FromSeconds(15);
            }
        }

        public bool RoleMappingsEnabled
        {
            get
            {
                var mappings = RoleMappings;
                return mappings.AllKeys.Any(key => !String.IsNullOrWhiteSpace(mappings.Get(key)));
            }
        }

        public NameValueCollection RoleMappings
        {
            get
            {
                return roleMappings;
            }
        }

        public string SymbolsPath
        {
            get
            {
                return MapPathFromAppSetting("symbolsPath", "~/App_Data/Symbols");
            }
        }

        public string ToolsPath
        {
            get
            {
                return MapPathFromAppSetting("debuggingToolsPath", "");
            }
        }

        public bool KeepSourcesCompressed
        {
            get
            {
                return GetFlagFromAppSetting("keepSourcesCompressed", true);
            }
        }

        public bool SynchronizeOnStart
        {
            get
            {
                return GetFlagFromAppSetting("synchronizeOnStart", true);
            }
        }

        public bool EnablePackageFileWatcher
        {
            get
            {
                return GetFlagFromAppSetting("enablePackageFileWatcher", true);
            }
        }

        public bool GroupPackageFilesById
        {
            get
            {
                return GetFlagFromAppSetting("groupPackageFilesById", true);
            }
        }

        public string LucenePackagesIndexPath
        {
            get
            {
                return MapPathFromAppSetting("lucenePath", "~/App_Data/Lucene");
            }
        }

        public string PackagesPath
        {
            get
            {
                return MapPathFromAppSetting("packagesPath", "~/App_Data/Packages");
            }
        }

        public PackageOverwriteMode PackageOverwriteMode
        {
            get { return GetEnumFromAppSetting("packageOverwriteMode", PackageOverwriteMode.Allow); }
        }

        public string LuceneUsersIndexPath
        {
            get
            {
                return Path.Combine(LucenePackagesIndexPath, "Users");
            }
        }

        public int LuceneMergeFactor
        {
            get
            {
                int value;
                return int.TryParse(GetAppSetting("luceneMergeFactor", "0"), out value) ? value : 0;
            }
        }

        protected bool GetFlagFromAppSetting(string key, bool defaultValue)
        {
            var flag = GetAppSetting(key, String.Empty);

            bool result;
            return Boolean.TryParse(flag ?? String.Empty, out result) ? result : defaultValue;
        }

        protected virtual string MapPathFromAppSetting(string key, string defaultValue)
        {
            var path = GetAppSetting(key, defaultValue);

            if (path.StartsWith("~/"))
            {
                path = Path.Combine(Environment.CurrentDirectory, path.Replace("~/", ""));
            }

            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        protected virtual T GetEnumFromAppSetting<T>(string appSetting, T defaultValue) where T : struct
        {
            T value;
            var parsed = Enum.TryParse(GetAppSetting(appSetting, defaultValue.ToString()), out value);
            return parsed ? value : defaultValue;
        }

        protected internal virtual string GetAppSetting(string key, string defaultValue)
        {
            var value = settings[GetAppSettingKey(key)];
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private string GetAppSettingKey(string key)
        {
            return prefix + key;
        }
    }
}
