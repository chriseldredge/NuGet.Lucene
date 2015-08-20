using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;

namespace NuGet.Lucene
{
    public class FastZipPackageFile : IPackageFile
    {
        private readonly IFastZipPackage fastZipPackage;
        private FrameworkName targetFramework;
        private string effectivePath;
        private bool targetFrameworkParsed;

        internal FastZipPackageFile(IFastZipPackage fastZipPackage, string path)
        {
            this.fastZipPackage = fastZipPackage;
            Path = Normalize(path);
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
            get { return TargetFramework != null ? effectivePath : Path; }
        }

        public FrameworkName TargetFramework
        {
            get
            {
                if (targetFrameworkParsed) return targetFramework;

                targetFrameworkParsed = true;

                var path = Uri.UnescapeDataString(Path);
                targetFramework = VersionUtility.ParseFrameworkNameFromFilePath(path, out effectivePath);
                if (targetFramework != VersionUtility.UnsupportedFrameworkName) return targetFramework;

                var parts = path.Split('/', '\\');

                if (parts.Length < 3)
                {
                    targetFramework = null;
                    return targetFramework;
                }

                var frameworkParts = parts[1].Split(new [] {'-'}, 2);
                var framework = frameworkParts[0];
                var version = "0.0";
                var profile = frameworkParts.Length == 2 ? frameworkParts[1] : "";

                for (var i = 0; i < framework.Length; i++)
                {
                    if (!Char.IsDigit(framework[i])) continue;

                    var chars = framework.Substring(i).ToCharArray();

                    framework = framework.Substring(0, i);

                    var sb = new StringBuilder();
                    sb.Append(chars[0]);

                    for (var k = 1; k < chars.Length; k++)
                    {
                        sb.Append('.');

                        if (k > 2)
                        {
                            sb.Append(chars, k, chars.Length - k);
                            break;
                        }

                        sb.Append(chars[k]);
                    }

                    if (sb.Length == 1)
                    {
                        sb.Append(".0");
                    }

                    version = sb.ToString();
                    break;
                }

                try
                {
                    targetFramework = new FrameworkName(framework, new Version(version), profile);
                }
                catch (ArgumentException)
                {
                }
                catch (FormatException)
                {
                }

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
