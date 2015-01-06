using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using NuGet.Lucene.Util;
using NuGet.Lucene.Web.Symbols;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// Provides PDB debugging symbol files to a debugger.
    /// </summary>
    public class SymbolsController : ApiControllerBase
    {
        public ISymbolSource SymbolSource { get; set; }

        public HttpResponseMessage GetFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }

            var stream = SymbolSource.OpenFile(path);

            if (stream == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var result = Request.CreateResponse(HttpStatusCode.OK);
            result.Content = new StreamContent(stream);
            return result;
        }

        public object GetSettings()
        {
            return new
            {
                SymbolServer = Url.GetSymbolsUri(),
                Enabled = SymbolSource.Enabled,
                SymbolsAvailable = SymbolSource.SymbolsAvailable
            };
        }

        [HttpPut]
        [Authorize(Roles = RoleNames.PackageManager)]
        public async Task<HttpResponseMessage> PutPackage([FromBody]IPackage package)
        {
            if (package == null || string.IsNullOrWhiteSpace(package.Id) || package.Version == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must provide package with valid id and version.");
            }

            if (!SymbolSource.Enabled)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotImplemented,
                    "Windows Debugging Tools are not configured.");
            }

            if (package.HasSourceAndSymbols())
            {
                Audit("Add symbols package {0} version {1}", package.Id, package.Version);
                await SymbolSource.AddSymbolsAsync(package, Url.GetSymbolSourceUri());
                return Request.CreateResponse(HttpStatusCode.OK);
            }

            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                "Symbol packages must contain source and PDB files under src and lib respectively.");
        }
    }
}
