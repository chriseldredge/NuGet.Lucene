using System;
using System.IO;
using System.Linq;

namespace NuGet.Lucene.Util
{
    public static class PackageExtensions
    {
        public static bool IsSymbolPackage(this IPackage package)
        {
            var hasSymbols = package.GetFiles("lib")
                .Any(pf => string.Equals(Path.GetExtension(pf.Path), ".pdb",
                    StringComparison.InvariantCultureIgnoreCase));

            return hasSymbols && package.GetFiles("src").Any();
        }
    }
}