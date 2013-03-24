using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using Lucene.Net.Linq;
using Ninject;
using Ninject.Components;
using Ninject.Modules;
using Ninject.Selection.Heuristics;
using Ninject.Web.Common;
using NuGet.Lucene.Web.Authentication;
using NuGet.Lucene.Web.Modules;

namespace NuGet.Lucene.Web
{
    public class ApplicationConfig : NinjectModule
    {
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
            Bind<Func<IKernel>>().ToMethod(ctx => () => Kernel);
            Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();
            
            Bind<ILucenePackageRepository>().ToConstant(cfg.Repository).OnDeactivation(_ => cfg.Dispose());
            Bind<LuceneDataProvider>().ToConstant(cfg.Provider);
            Bind<IQueryable<ApiUser>>().ToConstant(cfg.Provider.AsQueryable<ApiUser>());
            Bind<IApiKeyAuthentication>().To<LuceneApiKeyAuthentication>();

            Bind<IHttpModule>().To<ApiKeyAuthenticationModule>();

            var tokenSource = new ReusableCancellationTokenSource();
            Bind<ReusableCancellationTokenSource>().ToConstant(tokenSource);

            var repository = base.Kernel.Get<ILucenePackageRepository>();

            if (GetFlagFromAppSetting("synchronizeOnStart", true))
            {
                repository.SynchronizeWithFileSystem(tokenSource.Token);    
            }
        }

        public static bool ShowExceptionDetails
        {
            get { return GetFlagFromAppSetting("showExceptionDetails", false); }
        }
        
        public static bool EnableCrossDomainRequests
        {
            get { return GetFlagFromAppSetting("enableCrossDomainRequests", false); }
        }

        internal static bool GetFlagFromAppSetting(string key, bool defaultValue)
        {
            var flag = ConfigurationManager.AppSettings[key];

            bool result;
            return bool.TryParse(flag ?? string.Empty, out result) ? result : defaultValue;
        }

        internal static string MapPathFromAppSetting(string key, string defaultValue)
        {
            var path = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrWhiteSpace(path))
            {
                path = defaultValue;
            }

            if (path.StartsWith("~/"))
            {
                return HostingEnvironment.MapPath(path);
            }

            return path;
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