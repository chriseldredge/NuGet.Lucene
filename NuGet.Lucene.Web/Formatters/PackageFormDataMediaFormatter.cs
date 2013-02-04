using System.IO;
using System.Threading.Tasks;

namespace NuGet.Lucene.Web.Formatters
{
    public class PackageFormDataMediaFormatter : MultipartFormDataMediaFormatter<IPackage>
    {
        protected override Task<IPackage> ReadFormDataFromStreamAsync(Stream stream)
        {
            return Task.FromResult<IPackage>(new ZipPackage(stream));
        }
    }
}