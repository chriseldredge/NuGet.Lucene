using System.Net;
using System.Net.Http;
using System.Web.Http;
using NuGet.Lucene.Web.Symbols;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// Provides PDB debugging symbol files to a debugger.
    /// </summary>
    public class SymbolsController : ApiController
    {
        public ISymbolSource SymbolSource { get; set; }

        public HttpResponseMessage GetFile(string path)
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

        public object GetSettings()
        {
            return new
            {
                SymbolServer = Url.GetSymbolsUri(),
                Enabled = SymbolSource.Enabled,
                SymbolsAvailable = SymbolSource.SymbolsAvailable
            };
        }
    }
}