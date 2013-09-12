using System;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.Linq;
using System.ServiceModel.Web;
using Lucene.Net.Linq;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.DataServices
{
    /// <summary>
    /// WCF Data Services / OData provider for Lucene based NuGet package repository.
    /// </summary>
    public class PackageDataService : DataService<PackageDataSource>, IServiceProvider
    {
        public static PackageServiceStreamProvider PackageServiceStreamProvider { get; private set; }

        public IMirroringPackageRepository PackageRepository { get; set; }

        public static void InitializeService(DataServiceConfiguration config)
        {
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
            config.SetEntitySetAccessRule("Packages", EntitySetRights.AllRead);
            config.SetEntitySetPageSize("Packages", 40);
            config.UseVerboseErrors = NuGetWebApiModule.ShowExceptionDetails;
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

        [WebGet]
        public IQueryable<DataServicePackage> FindPackagesById(string id)
        {
            return PackageRepository.FindPackagesById(id)
                        .Select(AsDataServicePackage)
                        .AsQueryable();
        }

        [WebGet]
        public IQueryable<DataServicePackage> GetUpdates(string packageIds, string versions, bool includePrerelease, bool includeAllVersions, string targetFrameworks, string versionConstraints)
        {
            if (String.IsNullOrEmpty(packageIds) || String.IsNullOrEmpty(versions))
            {
                return Enumerable.Empty<DataServicePackage>().AsQueryable();
            }

            var idValues = packageIds.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var versionValues = versions.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var versionConstraintValues = string.IsNullOrEmpty(versionConstraints)
                                            ? new string[idValues.Length]
                                            : versionConstraints.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if ((idValues.Length == 0) || (idValues.Length != versionValues.Length))
            {
                // Exit early if the request looks invalid
                return Enumerable.Empty<DataServicePackage>().AsQueryable();
            }

            var packages = idValues
                .Zip(
                    versionValues.Select(v => new SemanticVersion(v)),
                    (id, version) => new PackageSpec {Id = id, Version = version})
                .ToList();

            var targetFrameworkValues = (targetFrameworks ?? "")
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(VersionUtility.ParseFrameworkName)
                .ToList();

            var versionSpecs = versionConstraintValues.Select((v,i) => string.IsNullOrWhiteSpace(v) ? new VersionSpec(packages[i].Version) : VersionUtility.ParseVersionSpec(v)).ToList();

            var updates = PackageRepository.GetUpdates(packages, includePrerelease, includeAllVersions, targetFrameworkValues, versionSpecs);
            return updates.Select(AsDataServicePackage).AsQueryable();
        }

        public static DataServicePackage AsDataServicePackage(IPackage package)
        {
            var lucenePackage = package as LucenePackage;

            if (lucenePackage != null)
                return new DataServicePackage(lucenePackage);

            var dataServicePackage = package as NuGet.DataServicePackage;
            
            if (dataServicePackage != null)
                return new DataServicePackage(dataServicePackage);

            throw new ArgumentException("Cannot convert package of type " + package.GetType() + " to DataServicePackage.");
        }

        protected override void OnStartProcessingRequest(ProcessRequestArgs args)
        {
            this.OperationContext = args.OperationContext;
        }

        protected DataServiceOperationContext OperationContext { get; set; }

        protected virtual Uri CurrentRequestUri
        {
            get { return OperationContext.AbsoluteRequestUri; }
        }

        protected virtual bool ClientDoesNotSpecifyOrder
        {
            get
            {
                var query = System.Web.HttpUtility.ParseQueryString(CurrentRequestUri.Query);

                return string.IsNullOrWhiteSpace(query["$orderby"]);
            }
        }
    }
}
