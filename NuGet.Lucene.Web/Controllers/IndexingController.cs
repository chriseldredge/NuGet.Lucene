using System.Web.Http;

namespace NuGet.Lucene.Web.Controllers
{
    public class IndexingController : ApiController
    {
        public ILucenePackageRepository Repository { get; set; }

        [HttpGet]
        public IndexingStatus Status()
        {
            return Repository.GetIndexingStatus();
        }
    }
}