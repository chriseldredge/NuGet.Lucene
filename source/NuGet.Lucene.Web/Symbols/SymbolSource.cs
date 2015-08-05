using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Lucene.IO;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Symbols
{
    public class SymbolSource : ISymbolSource
    {
        public string SymbolsPath { get; set; }
        public SymbolTools SymbolTools { get; set; }
        
        public bool Enabled
        {
            get { return SymbolTools.ToolsAvailable; }
        }

        public bool SymbolsAvailable
        {
            get { return Directory.Exists(SymbolsPath) && Directory.EnumerateDirectories(SymbolsPath, "*.pdb", SearchOption.TopDirectoryOnly).Any(); }
        }

        public bool AreSymbolsPresentFor(IPackageName package)
        {
            return File.Exists(GetNupkgPath(package));
        }

        public async Task AddSymbolsAsync(IPackage package, string symbolSourceUri)
        {
            try
            {
                await CopyNupkgToTargetPathAsync(package);
                await ProcessSymbolsAsync(package, symbolSourceUri);
            }
            finally
            {
                var disposablePackage = package as IDisposable;
                if (disposablePackage != null)
                {
                    disposablePackage.Dispose();
                }
            }
        }

        protected async Task CopyNupkgToTargetPathAsync(IPackage package)
        {
            var targetFile = new FileInfo(GetNupkgPath(package));

            if (targetFile.Directory != null && !targetFile.Directory.Exists)
            {
                targetFile.Directory.Create();
            }
            else if (targetFile.Exists)
            {
                targetFile.Delete();
            }

            var fastZipPackage = package as FastZipPackage;
            if (fastZipPackage != null)
            {
                File.Move(fastZipPackage.GetFileLocation(), targetFile.FullName);
                fastZipPackage.FileLocation = targetFile.FullName;
                return;
            }

            using (var sourceStream = package.GetStream())
            {
                using (var targetStream = targetFile.Open(FileMode.Create, FileAccess.Write))
                {
                    await sourceStream.CopyToAsync(targetStream);
                }
            }
        }

        public Task RemoveSymbolsAsync(IPackageName package)
        {
            var nupkgPath = GetNupkgPath(package);
            if (File.Exists(nupkgPath))
            {
                File.Delete(nupkgPath);
            }

            return Task.FromResult(true);
        }

        public Stream OpenFile(string relativePath)
        {
            var fullPath = Path.Combine(SymbolsPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                return null;
            }

            return File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenPackageSourceFile(IPackageName package, string relativePath)
        {
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

            var packagePath = GetNupkgPath(package);
            if (!File.Exists(packagePath)) return null;

            var srcPath = Path.Combine("src", relativePath);
            var packageFile = FastZipPackage.Open(packagePath, new byte[0]);

            var file = packageFile.GetFiles().SingleOrDefault(f => f.Path.Equals(srcPath, StringComparison.InvariantCultureIgnoreCase));
            return file != null ? new PackageDisposingStream(packageFile, file.GetStream()) : null;
        }

        public async Task ProcessSymbolsAsync(IPackage package, string symbolSourceUri)
        {
            var files = package.GetFiles().Where(f => f.Path.EndsWith("pdb", StringComparison.InvariantCultureIgnoreCase));

            using (var tempFolder = CreateTempFolderForPackage(package))
            {
                foreach (var file in files)
                {
                    var filePath = Path.Combine(tempFolder.Path, file.Path);
                    var fileDir = Path.GetDirectoryName(filePath);

                    if (fileDir != null && !Directory.Exists(fileDir))
                    {
                        Directory.CreateDirectory(fileDir);
                    }

                    using (var writeStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                    {
                        using (var readStream = file.GetStream())
                        {
                            await readStream.CopyToAsync(writeStream);
                        }
                    }

                    await ProcessSymbolFileAsync(package, filePath, symbolSourceUri);
                }
            }
        }

        public async Task ProcessSymbolFileAsync(IPackage package, string symbolFilePath, string symbolSourceUri)
        {
            var referencedSources = (await SymbolTools.GetSources(symbolFilePath)).ToList();

            var sourceFiles = new HashSet<string>(package.GetFiles("src").Select(f => f.Path.Substring(4)));

            if (referencedSources.Any() && sourceFiles.Any())
            {
                var sourceMapper = new SymbolSourceMapper();
                var mappings = sourceMapper.CreateSourceMappingIndex(package, symbolSourceUri, referencedSources, sourceFiles);

                await SymbolTools.MapSourcesAsync(symbolFilePath, mappings);
                await SymbolTools.IndexSymbolFile(package, symbolFilePath);
            }
        }

        public virtual string GetNupkgPath(IPackageName package)
        {
            return Path.Combine(SymbolsPath, package.Id, package.Id + "." + package.Version + ".symbols.nupkg");
        }

        protected virtual string GetTempFolderPathForPackage(IPackageName package)
        {
            return Path.Combine(SymbolsPath, package.Id + "-" + package.Version + ".tmp");
        }

        /// <summary>
        /// Creates a temp directory that gets deleted when the returned object is disposed
        /// </summary>
        /// <param name="package"></param>
        /// <returns>An IDisposable that deletes the folder when disposed</returns>
        protected TempFolder CreateTempFolderForPackage(IPackageName package)
        {
            return new TempFolder(GetTempFolderPathForPackage(package));
        }

        class PackageDisposingStream : ReadStream
        {
            private readonly IFastZipPackage package;

            public PackageDisposingStream(IFastZipPackage package, Stream stream) : base(stream)
            {
                this.package = package;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                package.Dispose();
            }
        }
    }
}
