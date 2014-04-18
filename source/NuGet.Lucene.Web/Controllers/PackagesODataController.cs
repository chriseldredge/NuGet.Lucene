using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData;
using NuGet.Lucene.Web.DataServices;

namespace NuGet.Lucene.Web.Controllers
{
    public class PackagesODataController : ODataController
    {
        public ILucenePackageRepository Repository { get; set; }

        [Queryable]
        public IQueryable<DataServices.DataServicePackage> GetPackages()
        {
            return Repository.LucenePackages.Select(AsDataServicePackage).AsQueryable();
        }


        public static DataServices.DataServicePackage AsDataServicePackage(IPackage package)
        {
            var lucenePackage = package as LucenePackage;

            if (lucenePackage != null)
                return new DataServices.DataServicePackage(lucenePackage);

            var dataServicePackage = package as NuGet.DataServicePackage;

            if (dataServicePackage != null)
                return new DataServices.DataServicePackage(dataServicePackage);

            throw new ArgumentException("Cannot convert package of type " + package.GetType() + " to DataServicePackage.");
        }

    }
}
