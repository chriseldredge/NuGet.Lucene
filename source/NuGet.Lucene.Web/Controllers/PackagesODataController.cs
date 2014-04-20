using System.Linq;
using System.Web.Http;
using System.Web.Http.OData;
using NuGet.Lucene.Web.Models;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// OData provider for Lucene based NuGet package repository.
    /// </summary>
    public class PackagesODataController : ODataController
    {
        public ILucenePackageRepository Repository { get; set; }

        [Queryable]
        public IQueryable<ODataPackage> GetPackagesOData()
        {
            return Repository.LucenePackages.Select(p => p.AsDataServicePackage()).AsQueryable();
        }
    }
}
