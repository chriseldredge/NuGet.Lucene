using System.Net;
using System.Net.Http;
using System.Web.Http;
using NuGet.Lucene.Web.Symbols;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// Provides source files to a debugger.
    /// </summary>
    public class SourceFilesController : ApiController
    {
        public ISymbolSource SymbolSource { get; set; }

        public HttpResponseMessage Get(string id, string version, string path)
        {
            var stream = SymbolSource.OpenPackageSourceFile(new PackageName(id, new SemanticVersion(version)), path);

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