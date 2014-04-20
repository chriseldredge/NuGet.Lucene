/*
using System.Linq;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.DataServices
{
    public class PackageDataSource
    {
        private readonly IMirroringPackageRepository packageRepository;
        private readonly IOperationContext operationContext;

        public PackageDataSource(IMirroringPackageRepository packageRepository, IOperationContext operationContext)
        {
            this.packageRepository = packageRepository;
            this.operationContext = operationContext;
        }

        public IQueryable<ODataPackage> Packages
        {
            get
            {
                OpportunisticallyPrefetchPackageFromOriginWhenNecessary();

                return packageRepository.GetPackages().Select(pkg => PackageDataService.AsDataServicePackage(pkg));
            }
        }

        /// <summary>
        /// When (a) the client is looking for a particular package version, and
        /// (b) package mirroring is enabled, and (c) the package is not in the
        /// local repository, mirror it before the query is executed.
        /// 
        /// This method enables automatic mirroring of packages during a package
        /// restore with nuget client version 2.7.
        /// </summary>
        private void OpportunisticallyPrefetchPackageFromOriginWhenNecessary()
        {
            if (!packageRepository.MirroringEnabled) return;

            string packageId;
            SemanticVersion packageVersion;

            if (!operationContext.IsQueryForSpecificPackage(out packageId, out packageVersion)) return;

            // IMirroringPackageRepository.FindPackage will mirror the package when missing.
            packageRepository.FindPackage(packageId, packageVersion);
        }
    }
}
*/