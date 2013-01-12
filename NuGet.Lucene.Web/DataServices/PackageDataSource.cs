using System.Linq;

namespace NuGet.Lucene.Web.DataServices
{
    public class PackageDataSource
    {
        private readonly ILucenePackageRepository packageRepository;

        public PackageDataSource(ILucenePackageRepository packageRepository)
        {
            this.packageRepository = packageRepository;
        }

        public IQueryable<DataServicePackage> Packages
        {
            get
            {
                return packageRepository.LucenePackages.Select(pkg => PackageDataService.AsDataServicePackage(pkg));
            }
        }
    }
}