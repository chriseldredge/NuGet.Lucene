using System;
using System.IO;
using System.Linq;

namespace NuGet.Lucene.Util
{
    public static class FileSystemExtensions
    {
        public static string MakeRelative(this IFileSystem fileSystem, string path)
        {
            if (!Path.IsPathRooted(path)) return path;

            var root = fileSystem.Root;
            if (root.Last() != Path.DirectorySeparatorChar)
            {
                root += Path.DirectorySeparatorChar;
            }

            if (path.StartsWith(root, StringComparison.InvariantCultureIgnoreCase))
            {
                return path.Substring(root.Length);
            }

            throw new ArgumentException("The path " + path + " is not rooted in " + root);
        }
    }
}