using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Symbols
{
    public class SymbolSource : ISymbolSource
    {
        public string SymbolsPath { get; set; }
        public SymbolTools SymbolTools { get; set; }

        public bool KeepSourcesCompressed { get; set; }

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
            return File.Exists(GetNupkgPath(package))
                || Directory.Exists(GetUnzippedPackagePath(package));
        }

        public async Task AddSymbolsAsync(IPackage package, string symbolSourceUri)
        {
            // Copy the package unmodified to the target path if KeepSourcesCompressed is true
            if (KeepSourcesCompressed)
            {
                await CopyNupkgToTargetPathAsync(package);
            }
            else
            {
                await UnzipPackageAsync(package);
            }
            await ProcessSymbolsAsync(package, symbolSourceUri);
        }

        protected async Task CopyNupkgToTargetPathAsync(IPackage package)
        {
            var targetFile = new FileInfo(GetNupkgPath(package));

            if (targetFile.Directory != null && !targetFile.Directory.Exists)
            {
                targetFile.Directory.Create();
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

            var folderPath = GetUnzippedPackagePath(package);
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, recursive: true);
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

            // Try to return the source file from its uncompressed location
            var parts = new[] {
                package.Id,
                package.Version.ToString(),
                "src",
                relativePath
            };

            var stream = OpenFile(Path.Combine(parts));

            if (stream != null) return stream;

            // If the file wasn't found uncompressed look for it in symbol package zip file
            var packagePath = GetNupkgPath(package);
            if (!File.Exists(packagePath)) return null;

            var srcPath = Path.Combine("src", relativePath);
            var packageFile = new ZipPackage(packagePath);
            var file = packageFile.GetFiles().SingleOrDefault(f => f.Path.Equals(srcPath, StringComparison.InvariantCultureIgnoreCase));
            return file != null ? file.GetStream() : null;
        }

        public async Task UnzipPackageAsync(IPackage package)
        {
            var dir = GetUnzippedPackagePath(package);

            Directory.CreateDirectory(dir);
            var visitedDirectories = new HashSet<string>();

            foreach (var file in package.GetFiles())
            {
                var filePath = Path.Combine(dir, file.Path);
                var fileDir = Path.GetDirectoryName(filePath);

                if (fileDir != null && !visitedDirectories.Contains(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                    visitedDirectories.Add(fileDir);
                }

                using (var writeStream = File.OpenWrite(filePath))
                {
                    using (var readStream = file.GetStream())
                    {
                        await readStream.CopyToAsync(writeStream);
                    }
                }
            }
        }

        public async Task ProcessSymbolsAsync(IPackage package, string symbolSourceUri)
        {
            var files = package.GetLibFiles().Where(f => f.Path.EndsWith("pdb", StringComparison.InvariantCultureIgnoreCase));

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

        /// <summary>
        /// Returns the path a package should be decompressed to
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public virtual string GetUnzippedPackagePath(IPackageName package)
        {
            return Path.Combine(SymbolsPath, package.Id, package.Version.ToString());
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
    }
}