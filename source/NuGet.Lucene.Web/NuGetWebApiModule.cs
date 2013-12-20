using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using Lucene.Net.Linq;
using Lucene.Net.Store;
using Ninject;
using Ninject.Components;
using Ninject.Modules;
using Ninject.Selection.Heuristics;
using NuGet.Lucene.Web.Authentication;
using NuGet.Lucene.Web.Models;
using NuGet.Lucene.Web.Modules;
using Version = Lucene.Net.Util.Version;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiModule : NinjectModule
    {
        public const string AppSettingNamesapce = "NuGet.Lucene.Web:";
        public const string DefaultRoutePathPrefix = "api/";

        public override void Load()
        {
            var cfg = new LuceneRepositoryConfigurator
                {
                    EnablePackageFileWatcher = GetFlagFromAppSetting("enablePackageFileWatcher", true),
                    GroupPackageFilesById = GetFlagFromAppSetting("groupPackageFilesById", true),
                    LuceneIndexPath = MapPathFromAppSetting("lucenePath", "~/App_Data/Lucene"),
                    PackagePath = MapPathFromAppSetting("packagesPath", "~/App_Data/Packages")
                };

            cfg.Initialize();

            Kernel.Components.Add<IInjectionHeuristic, NonDecoratedPropertyInjectionHeuristic>();

            var routeMapper = new NuGetWebApiRouteMapper(RoutePathPrefix);
            var mirroringPackageRepository = MirroringPackageRepositoryFactory.Create(cfg.Repository, PackageMirrorTargetUrl, PackageMirrorTimeout);
            var usersDataProvider = InitializeUsersDataProvider();

            Bind<NuGetWebApiRouteMapper>().ToConstant(routeMapper);
            Bind<ILucenePackageRepository>().ToConstant(cfg.Repository).OnDeactivation(_ => cfg.Dispose());
            Bind<IMirroringPackageRepository>().ToConstant(mirroringPackageRepository);
            Bind<LuceneDataProvider>().ToConstant(cfg.Provider);
            Bind<UserStore>().ToConstant(new UserStore(usersDataProvider));
            Bind<IApiKeyAuthentication>().To<LuceneApiKeyAuthentication>();

            Bind<IHttpModule>().To<ApiKeyAuthenticationModule>();
            Bind<IHttpModule>().To<LocalRequstAuthenticationModule>();

            var tokenSource = new ReusableCancellationTokenSource();
            Bind<ReusableCancellationTokenSource>().ToConstant(tokenSource);

            var repository = base.Kernel.Get<ILucenePackageRepository>();

            if (GetFlagFromAppSetting("synchronizeOnStart", true))
            {
                repository.SynchronizeWithFileSystem(tokenSource.Token);    
            }
        }

        private LuceneDataProvider InitializeUsersDataProvider()
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

        public static string RoutePathPrefix
        {
            get { return GetAppSetting("routePathPrefix", DefaultRoutePathPrefix); }
        }

        public static string PackageMirrorTargetUrl
        {
            get { return GetAppSetting("packageMirrorTargetUrl", string.Empty); }
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
                return HostingEnvironment.MapPath(path);
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

    public class NonDecoratedPropertyInjectionHeuristic : NinjectComponent, IInjectionHeuristic
    {
        private readonly IKernel kernel;

        private static readonly ISet<Assembly> knownAssemblies = new HashSet<Assembly> { typeof(NonDecoratedPropertyInjectionHeuristic).Assembly };

        public NonDecoratedPropertyInjectionHeuristic(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public bool ShouldInject(MemberInfo memberInfo)
        {
            var propertyInfo = memberInfo as PropertyInfo;
            return ShouldInject(propertyInfo);
        }

        private bool ShouldInject(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                return false;

            if (!propertyInfo.CanWrite)
                return false;

            var targetType = propertyInfo.ReflectedType;
            var assembly = targetType.Assembly;
            if (!knownAssemblies.Contains(assembly))
                return false;

            var instance = kernel.TryGet(propertyInfo.PropertyType);
            return instance != null;
        }
    }
}