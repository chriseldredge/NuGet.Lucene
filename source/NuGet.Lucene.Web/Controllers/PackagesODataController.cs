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
        public IMirroringPackageRepository MirroringRepository { get; set; }

        [Queryable]
        public IQueryable<ODataPackage> Get()
        {
            return Repository.LucenePackages.Select(p => p.AsDataServicePackage()).AsQueryable();
        }

        public object Get([FromODataUri] string id, [FromODataUri] string version)
        {
            SemanticVersion semanticVersion;
            if (!SemanticVersion.TryParse(version, out semanticVersion))
            {
                return BadRequest("Invalid version");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Invalid package id");
            }

            var package = MirroringRepository.FindPackage(id, semanticVersion);

            return package == null ? (object)NotFound() : package.AsDataServicePackage();
        }
    }
}
