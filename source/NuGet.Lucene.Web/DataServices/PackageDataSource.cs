using System.Linq;

namespace NuGet.Lucene.Web.DataServices
{
    public class PackageDataSource
    {
        private readonly IPackageRepository packageRepository;

        public PackageDataSource(IPackageRepository packageRepository)
        {
            this.packageRepository = packageRepository;
        }

        public IQueryable<DataServicePackage> Packages
        {
            get
            {
                return packageRepository.GetPackages().Select(pkg => PackageDataService.AsDataServicePackage(pkg));
            }
        }
    }
}