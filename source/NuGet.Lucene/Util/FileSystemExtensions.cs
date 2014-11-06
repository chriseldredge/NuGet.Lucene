using System;
using System.IO;
using System.Linq;

namespace NuGet.Lucene.Util
{
    public static class FileSystemExtensions
    {
        public const string TempFolderName = ".tmp";

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

        public static string GetTempFolder(this IFileSystem fileSystem)
        {
            return fileSystem.GetFullPath(TempFolderName);
        }

        public static bool IsTempFile(this IFileSystem fileSystem, string path)
        {
            return Path.GetExtension(path) == ".tmp" ||
                path.Split(Path.DirectorySeparatorChar).Any(p => string.Equals(p, TempFolderName));
        }
    }
}
