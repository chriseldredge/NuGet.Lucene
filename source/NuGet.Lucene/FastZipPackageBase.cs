using System;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace NuGet.Lucene
{
    public abstract class FastZipPackageBase
    {
        private ZipFile zipFile;

        public abstract Stream GetStream();

        public void Dispose()
        {
            if (zipFile == null) return;
            zipFile.Close();
            zipFile = null;
        }

        public Stream GetZipEntryStream(string path)
        {
            if (zipFile == null)
            {
                zipFile = new ZipFile(GetStream());
            }

            var zipEntry = zipFile.GetEntry(path.Replace('\\', '/'));
            if (zipEntry == null)
            {
                throw new ArgumentException("Zip file does not contain requested entry.", "path");
            }

            return zipFile.GetInputStream(zipEntry);
        }

        public static DateTimeOffset GetPackageCreatedDateTime(Stream stream)
        {
            var zip = new ZipFile(stream);

            return zip.Cast<ZipEntry>()
                .Where(f => f.Name.EndsWith(".nuspec"))
                .Select(f => f.DateTime)
                .DefaultIfEmpty(default(DateTime).ToUniversalTime())
                .First();

        }

        public void ExtractContents(IFileSystem fileSystem, string extractPath)
        {
            throw new NotSupportedException();
        }
    }
}
