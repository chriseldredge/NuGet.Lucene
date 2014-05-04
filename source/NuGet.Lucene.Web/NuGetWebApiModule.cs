using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Autofac;
using Lucene.Net.Linq;
using Lucene.Net.Store;
using NuGet.Lucene.Web.Authentication;
using NuGet.Lucene.Web.Models;
using NuGet.Lucene.Web.Symbols;
using Version = Lucene.Net.Util.Version;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiModule : Module
    {
        public const string AppSettingNamesapce = "NuGet.Lucene.Web:";
        public const string DefaultRoutePathPrefix = "api/";

        protected override void Load(ContainerBuilder builder)
        {
            var cfg = new LuceneRepositoryConfigurator
                {
                    EnablePackageFileWatcher = GetFlagFromAppSetting("enablePackageFileWatcher", true),
                    GroupPackageFilesById = GetFlagFromAppSetting("groupPackageFilesById", true),
                    LuceneIndexPath = MapPathFromAppSetting("lucenePath", "~/App_Data/Lucene"),
                    PackagePath = MapPathFromAppSetting("packagesPath", "~/App_Data/Packages")
                };

            cfg.Initialize();

            var routeMapper = new NuGetWebApiRouteMapper(RoutePathPrefix);
            var mirroringPackageRepository = MirroringPackageRepositoryFactory.Create(
                cfg.Repository, PackageMirrorTargetUrl, PackageMirrorTimeout, AlwaysCheckMirror);
            var userStore = InitializeUserStore();

            builder.RegisterInstance(routeMapper);
            builder.RegisterInstance(cfg.Repository).As<ILucenePackageRepository>();
            //RegisterInstance(cfg.Repository).OnDeactivation(_ => cfg.Dispose());
            builder.RegisterInstance(mirroringPackageRepository).As<IMirroringPackageRepository>();
            builder.RegisterInstance(cfg.Provider).As<LuceneDataProvider>();
            builder.RegisterInstance(userStore).As<UserStore>();

            var symbolsPath = MapPathFromAppSetting("symbolsPath", "~/App_Data/Symbols");
            builder.RegisterInstance(new SymbolSource { SymbolsPath = symbolsPath }).As<ISymbolSource>();
            builder.RegisterInstance(new SymbolTools
            {
                SymbolPath = symbolsPath,
                ToolPath = MapPathFromAppSetting("debuggingToolsPath", "")
            });

            LoadAuthentication(builder);

            var tokenSource = new ReusableCancellationTokenSource();
            builder.RegisterInstance(tokenSource);

            //TODO: this should move to somewhere else.
            var repository = cfg.Repository;

            if (GetFlagFromAppSetting("synchronizeOnStart", true))
            {
                repository.SynchronizeWithFileSystem(tokenSource.Token);    
            }
        }

        public virtual void LoadAuthentication(ContainerBuilder builder)
        {
            builder.Register(_ => new LuceneApiKeyAuthentication()).As<IApiKeyAuthentication>();
            /*
            Bind<IHttpModule>().To<ApiKeyAuthenticationModule>();

            if (AllowAnonymousPackageChanges)
            {
                Bind<IHttpModule>().To<AnonymousPackageManagerModule>();
            }

            if (HandleLocalRequestsAsAdmin)
            {
                Bind<IHttpModule>().To<LocalRequestAuthenticationModule>();
            }

            if (RoleMappingsEnabled)
            {
                Bind<IHttpModule>().To<RoleMappingAuthenticationModule>();
            }
             * */
        }

        public virtual UserStore InitializeUserStore()
        {
            var usersDataProvider = InitializeUsersDataProvider();
            var userStore = new UserStore(usersDataProvider)
            {
                LocalAdministratorApiKey = GetAppSetting("localAdministratorApiKey", string.Empty),
                HandleLocalRequestsAsAdmin = HandleLocalRequestsAsAdmin
            };
            userStore.Initialize();
            return userStore;
        }

        public virtual LuceneDataProvider InitializeUsersDataProvider()
        {
            var usersIndexPath = Path.Combine(MapPathFromAppSetting("lucenePath", "~/App_Data/Lucene"), "Users");
            var directoryInfo = new DirectoryInfo(usersIndexPath);
            var dir = FSDirectory.Open(directoryInfo, new NativeFSLockFactory(directoryInfo));
            var provider = new LuceneDataProvider(dir, Version.LUCENE_30);
            provider.Settings.EnableMultipleEntities = false;
            return provider;
        }

        public static bool ShowExceptionDetails
        {
            get { return GetFlagFromAppSetting("showExceptionDetails", false); }
        }
        
        public static bool EnableCrossDomainRequests
        {
            get { return GetFlagFromAppSetting("enableCrossDomainRequests", false); }
        }

        public static bool HandleLocalRequestsAsAdmin
        {
            get { return GetFlagFromAppSetting("handleLocalRequestsAsAdmin", false); }
        }

        public static bool AllowAnonymousPackageChanges
        {
            get { return GetFlagFromAppSetting("allowAnonymousPackageChanges", false); }
        }

        public static string RoutePathPrefix
        {
            get { return GetAppSetting("routePathPrefix", DefaultRoutePathPrefix); }
        }

        public static string PackageMirrorTargetUrl
        {
            get { return GetAppSetting("packageMirrorTargetUrl", string.Empty); }
        }

        public static bool AlwaysCheckMirror
        {
            get { return GetFlagFromAppSetting("alwaysCheckMirror", false); }
        }

        public static TimeSpan PackageMirrorTimeout
        {
            get
            {
                var str = GetAppSetting("packageMirrorTimeout", "0:00:15");
                TimeSpan ts;
                return TimeSpan.TryParse(str, out ts) ? ts : TimeSpan.FromSeconds(15);
            }
        }

        public static bool RoleMappingsEnabled
        {
            get
            {
                var mappings = RoleMappings;
                return mappings.AllKeys.Any(key => !string.IsNullOrWhiteSpace(mappings.Get(key)));
            }
        }

        public static NameValueCollection RoleMappings
        {
            get
            {
                var mappings = ConfigurationManager.GetSection("roleMappings") as NameValueCollection;
                return mappings ?? new NameValueCollection();
            }
        }

        internal static bool GetFlagFromAppSetting(string key, bool defaultValue)
        {
            var flag = GetAppSetting(key, string.Empty);

            bool result;
            return bool.TryParse(flag ?? string.Empty, out result) ? result : defaultValue;
        }
        
        internal static string MapPathFromAppSetting(string key, string defaultValue)
        {
            var path = GetAppSetting(key, defaultValue);

            if (path.StartsWith("~/"))
            {
                return HostingEnvironment.IsHosted
                    ? HostingEnvironment.MapPath(path)
                    : Path.Combine(Environment.CurrentDirectory, path.Replace("~/", ""));
            }

            return path;
        }

        internal static string GetAppSetting(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[GetAppSettingKey(key)];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static string GetAppSettingKey(string key)
        {
            return AppSettingNamesapce + key;
        }
    }
}