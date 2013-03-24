using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.Linq;
using System.ServiceModel.Web;
using Lucene.Net.Linq;

namespace NuGet.Lucene.Web.DataServices
{
    public class PackageDataService : DataService<PackageDataSource>, IServiceProvider
    {
        public static PackageServiceStreamProvider PackageServiceStreamProvider { get; private set; }

        public ILucenePackageRepository PackageRepository { get; set; }

        public static void InitializeService(DataServiceConfiguration config)
        {
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
            config.SetEntitySetAccessRule("Packages", EntitySetRights.AllRead);
            config.SetEntitySetPageSize("Packages", 40);
            config.UseVerboseErrors = ApplicationConfig.ShowExceptionDetails;
            RegisterServices(config);
        }

        internal static void RegisterServices(IDataServiceConfiguration config)
        {
            PackageServiceStreamProvider = new PackageServiceStreamProvider();
            config.SetServiceOperationAccessRule("Search", ServiceOperationRights.AllRead);
            config.SetServiceOperationAccessRule("FindPackagesById", ServiceOperationRights.AllRead);
            config.SetServiceOperationAccessRule("GetUpdates", ServiceOperationRights.AllRead);
        }

        protected override void HandleException(HandleExceptionArgs args)
        {
            UnhandledExceptionLogger.Log.Error(args.Exception);
        }

        protected override PackageDataSource CreateDataSource()
        {
            return new PackageDataSource(PackageRepository);
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IDataServiceStreamProvider))
            {
                return PackageServiceStreamProvider;
            }

            return null;
        }

        protected override void OnStartProcessingRequest(ProcessRequestArgs args)
        {
            this.OperationContext = args.OperationContext;
        }

        protected DataServiceOperationContext OperationContext { get; set; }

        [WebGet]
        public IQueryable<DataServicePackage> Search(string searchTerm, string targetFramework, bool includePrerelease)
        {
            var targetFrameworks = Enumerable.Empty<string>();

            if (!string.IsNullOrWhiteSpace(targetFramework))
            {
                targetFrameworks = targetFramework.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Distinct();
            }

            var searchQuery = PackageRepository.Search(searchTerm, targetFrameworks, includePrerelease);

            if (ClientDoesNotSpecifyOrder)
            {
                searchQuery = searchQuery.OrderBy(result => result.Score());
            }

            return from package in searchQuery select AsDataServicePackage(package);
        }

        private bool ClientDoesNotSpecifyOrder
        {
            get
            {
                var query = System.Web.HttpUtility.ParseQueryString(OperationContext.AbsoluteRequestUri.Query);

                return string.IsNullOrWhiteSpace(query["$orderby"]);
            }
        }

        [WebGet]
        public IQueryable<DataServicePackage> FindPackagesById(string id)
        {
            return PackageRepository.FindPackagesById(id)
                                    .Select(AsDataServicePackage)
                                    .AsQueryable();
        }

        [WebGet]
        public IQueryable<DataServicePackage> GetUpdates(string packageIds, string versions, bool includePrerelease, bool includeAllVersions, string targetFrameworks)
        {
            if (String.IsNullOrEmpty(packageIds) || String.IsNullOrEmpty(versions))
            {
                return Enumerable.Empty<DataServicePackage>().AsQueryable();
            }

            var idValues = packageIds.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var versionValues = versions.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var targetFrameworkValues = String.IsNullOrEmpty(targetFrameworks) ? null :
                                                                                 targetFrameworks.Split('|').Select(VersionUtility.ParseFrameworkName).ToList();

            if ((idValues.Length == 0) || (idValues.Length != versionValues.Length))
            {
                // Exit early if the request looks invalid
                return Enumerable.Empty<DataServicePackage>().AsQueryable();
            }

            var packagesToUpdate = new List<IPackageMetadata>();
            for (int i = 0; i < idValues.Length; i++)
            {
                packagesToUpdate.Add(new PackageBuilder { Id = idValues[i], Version = new SemanticVersion(versionValues[i]) });
            }

            return from package in PackageRepository.GetUpdatesCore(packagesToUpdate, includePrerelease, includeAllVersions, targetFrameworkValues).AsQueryable()
                   select AsDataServicePackage(package);
        }

        public static DataServicePackage AsDataServicePackage(IPackage package)
        {
            return new DataServicePackage((LucenePackage)package);
        }
    }
}
