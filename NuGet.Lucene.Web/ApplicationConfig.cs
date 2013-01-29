using System.Configuration;
using System.Threading;
using System.Web.Hosting;
using Ninject;
using Ninject.Modules;

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

            Bind<ILucenePackageRepository>().ToConstant(cfg.Repository).OnDeactivation(_ => cfg.Dispose());

            var repository = base.Kernel.Get<ILucenePackageRepository>();

            if (GetFlagFromAppSetting("synchronizeOnStart", true))
            {
                repository.SynchronizeWithFileSystem(CancellationToken.None);    
            }
        }

        public static bool ShowExceptionDetails
        {
            get { return GetFlagFromAppSetting("showExceptionDetails", false); }
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
}