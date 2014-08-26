using System.IO;
using Autofac;
using Autofac.Integration.WebApi;
using Lucene.Net.Linq;
using Lucene.Net.Store;
using NuGet.Lucene.Web.Authentication;
using NuGet.Lucene.Web.Models;
using NuGet.Lucene.Web.Middleware;
using NuGet.Lucene.Web.Symbols;
using Version = Lucene.Net.Util.Version;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiModule : Module
    {
        private readonly INuGetWebApiSettings settings;

        public NuGetWebApiModule()
            : this(new NuGetWebApiSettings())
        {
        }

        public NuGetWebApiModule(INuGetWebApiSettings settings)
        {
            this.settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(settings).As<INuGetWebApiSettings>();

            var routeMapper = new NuGetWebApiRouteMapper(settings.RoutePathPrefix);
            var configurator = InitializeRepositoryConfigurator(settings);
            var userStore = InitializeUserStore(settings);
            var repository = configurator.Repository;
            var mirroringPackageRepository = MirroringPackageRepositoryFactory.Create(
                repository, settings.PackageMirrorTargetUrl, settings.PackageMirrorTimeout, settings.AlwaysCheckMirror);

            builder.RegisterInstance(configurator);
            builder.RegisterInstance(repository);

            builder.RegisterInstance(routeMapper);
            builder.RegisterInstance(mirroringPackageRepository).As<IMirroringPackageRepository>();
            builder.RegisterInstance(userStore);

            var symbolsPath = settings.SymbolsPath;
            builder.RegisterInstance(new SymbolSource
            {
                SymbolsPath = symbolsPath,
                KeepSourcesCompressed = settings.KeepSourcesCompressed
            }).As<ISymbolSource>().PropertiesAutowired();
            builder.RegisterInstance(new SymbolTools
            {
                SymbolPath = symbolsPath,
                ToolPath = settings.ToolsPath
            });

            LoadAuthMiddleware(builder, settings);

            builder.RegisterInstance(new StopSynchronizationCancellationTokenSource());

            builder.RegisterApiControllers(typeof (NuGetWebApiModule).Assembly).PropertiesAutowired();
        }

        protected virtual ILuceneRepositoryConfigurator InitializeRepositoryConfigurator(INuGetWebApiSettings settings)
        {
            var cfg = new LuceneRepositoryConfigurator
            {
                EnablePackageFileWatcher = settings.EnablePackageFileWatcher,
                GroupPackageFilesById = settings.GroupPackageFilesById,
                LuceneIndexPath = settings.LucenePackagesIndexPath,
                PackagePath = settings.PackagesPath
            };

            cfg.Initialize();

            return cfg;
        }

        protected virtual void LoadAuthMiddleware(ContainerBuilder builder, INuGetWebApiSettings settings)
        {
            builder.RegisterType<LuceneApiKeyAuthentication>().As<IApiKeyAuthentication>().PropertiesAutowired();
            builder.RegisterType<ApiKeyAuthenticationMiddleware>().InstancePerRequest().PropertiesAutowired();

            if (settings.HandleLocalRequestsAsAdmin)
            {
                builder.RegisterType<LocalRequestAuthenticationMiddleware>().InstancePerRequest().PropertiesAutowired();
            }

            if (settings.AllowAnonymousPackageChanges)
            {
                builder.RegisterType<AnonymousPackageManagerMiddleware>().InstancePerRequest().PropertiesAutowired();
            }

            if (settings.RoleMappingsEnabled)
            {
                builder.RegisterType<RoleMappingAuthenticationMiddleware>().InstancePerRequest().PropertiesAutowired();
            }
        }

        protected virtual UserStore InitializeUserStore(INuGetWebApiSettings settings)
        {
            var usersDataProvider = InitializeUsersDataProvider(settings);
            var userStore = new UserStore(usersDataProvider)
            {
                LocalAdministratorApiKey = settings.LocalAdministratorApiKey,
                HandleLocalRequestsAsAdmin = settings.HandleLocalRequestsAsAdmin
            };
            userStore.Initialize();
            return userStore;
        }

        protected virtual LuceneDataProvider InitializeUsersDataProvider(INuGetWebApiSettings settings)
        {
            var usersIndexPath = settings.LuceneUsersIndexPath;
            var directoryInfo = new DirectoryInfo(usersIndexPath);
            var dir = FSDirectory.Open(directoryInfo, new NativeFSLockFactory(directoryInfo));
            var provider = new LuceneDataProvider(dir, Version.LUCENE_30);
            provider.Settings.EnableMultipleEntities = false;
            return provider;
        }
    }
}
