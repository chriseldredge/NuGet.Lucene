using System.Net;
using System.Net.Http;
using System.Web.Http;
using NuGet.Lucene.Web.Symbols;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// Provides PDB debugging symbol files to a debugger.
    /// </summary>
    public class SymbolsController : ApiController
    {
        public ISymbolSource SymbolSource { get; set; }

        public HttpResponseMessage Get(string path)
        {
            var stream = SymbolSource.OpenFile(path);

            if (stream == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var result = Request.CreateResponse(HttpStatusCode.OK);
            result.Content = new StreamContent(stream);
            return result;
        }
    }
}