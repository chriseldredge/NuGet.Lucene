using System.IO;
using System.Threading.Tasks;

namespace NuGet.Lucene.Web.Symbols
{
    public interface ISymbolSource
    {
        Task AddSymbolsAsync(IPackage package, string symbolSourceUri);
        Task RemoveSymbolsAsync(IPackageName package);
        Stream OpenFile(string relativePath);
        Stream OpenPackageSourceFile(IPackageName package, string relativePath);
        bool Enabled { get; }
        bool SymbolsAvailable { get; }
        bool AreSymbolsPresentFor(IPackageName package);
    }
}