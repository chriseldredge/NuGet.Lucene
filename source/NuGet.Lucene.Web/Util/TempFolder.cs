using System;
using System.IO;

namespace NuGet.Lucene.Web.Util {
    public class TempFolder : IDisposable {
        public string Path { get; private set; }

        public TempFolder(string path) {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public void Dispose() {
            if (Directory.Exists(Path)) {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
