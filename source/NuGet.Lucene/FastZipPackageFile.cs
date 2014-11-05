using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace NuGet.Lucene
{
    public class FastZipPackageFile : IPackageFile
    {
        private readonly IFastZipPackage fastZipPackage;
        private readonly FrameworkName targetFramework;

        internal FastZipPackageFile(IFastZipPackage fastZipPackage, string path)
        {
            this.fastZipPackage = fastZipPackage;
            Path = path;

            string effectivePath;
            targetFramework = VersionUtility.ParseFrameworkNameFromFilePath(Normalize(path), out effectivePath);
            EffectivePath = effectivePath;
        }

        private string Normalize(string path)
        {
            return path
                .Replace('/', System.IO.Path.DirectorySeparatorChar)
                .TrimStart(System.IO.Path.DirectorySeparatorChar);
        }

        public string Path
        {
            get;
            private set;
        }

        public string EffectivePath
        {
            get;
            private set;
        }

        public FrameworkName TargetFramework
        {
            get
            {
                return targetFramework;
            }
        }

        IEnumerable<FrameworkName> IFrameworkTargetable.SupportedFrameworks
        {
            get
            {
                if (TargetFramework != null)
                {
                    yield return TargetFramework;
                }
            }
        }

        public Stream GetStream()
        {
            return fastZipPackage.GetZipEntryStream(Path);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
