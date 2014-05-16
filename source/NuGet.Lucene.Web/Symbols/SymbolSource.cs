using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet.Lucene.Web.Symbols
{
    public class SymbolSource : ISymbolSource {
        public const string TempSubFolderName = "DecompressTemp";

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
            var path = GetPackageSymbolPath(package);

            return KeepSourcesCompressed ? File.Exists(path) : Directory.Exists(path);
        }

        public async Task AddSymbolsAsync(IPackage package, string symbolSourceUri)
        {
            try {
                // Copy the package unmodified to the target path if KeepSourcesCompressed is true
                await CopyNupkgToTargetPathIfNecessary(package);
                await UnzipPackageAsync(package);
                await ProcessSymbolsAsync(package, symbolSourceUri);
            } finally {
                string unzippedPackageSymbolPath = GetUnzippedPackageSymbolPath(package);
                if (KeepSourcesCompressed && Directory.Exists(unzippedPackageSymbolPath)) {
                    Directory.Delete(unzippedPackageSymbolPath, recursive: true);
                }
            }
        }

        protected Task CopyNupkgToTargetPathIfNecessary(IPackage package) {
            if (KeepSourcesCompressed) {
                var targetFile = new FileInfo(GetPackageSymbolPath(package));
                try {
                    if(targetFile.Directory != null && !targetFile.Directory.Exists) targetFile.Directory.Create();
                    using (var sourceStream = package.GetStream()) {
                        using (var targetStream = targetFile.Open(FileMode.Create, FileAccess.Write)) {
                            sourceStream.CopyTo(targetStream);
                        }
                    }
                } catch (IOException ex) {
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(true);
        }

        public Task RemoveSymbolsAsync(IPackageName package)
        {
            var path = GetPackageSymbolPath(package);
            if (KeepSourcesCompressed) {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            } else {
                if (Directory.Exists(path)) {
                    Directory.Delete(path, recursive: true);
                }
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

        public Stream OpenPackageSourceFile(IPackageName package, string relativePath) {
            var packagePath = GetPackageSymbolPath(package);

            if (KeepSourcesCompressed) {
                try {
                    var packageFile = new ZipPackage(packagePath);
                    var file = packageFile.GetFiles().SingleOrDefault(f => f.Path.Equals(Path.Combine("src", relativePath), StringComparison.InvariantCultureIgnoreCase));
                    return file != null ? file.GetStream() : null;
                } catch (IOException ex) {
                    return null;
                }
            } else {
                var parts = new[] {
                    package.Id,
                    package.Version.ToString(),
                    "src",
                    relativePath
                };

                return OpenFile(Path.Combine(parts));
            }
        }

        public async Task UnzipPackageAsync(IPackage package)
        {
            var dir = GetUnzippedPackageSymbolPath(package);

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

        public async Task ProcessSymbolsAsync(IPackageName package, string symbolSourceUri)
        {
            var packageSymbolsDir = Path.Combine(GetUnzippedPackageSymbolPath(package), "lib");
            var files = Directory.EnumerateFiles(packageSymbolsDir, "*.pdb", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                await ProcessSymbolFileAsync(package, file, symbolSourceUri);
            }
        }

        public async Task ProcessSymbolFileAsync(IPackageName package, string symbolFilePath, string symbolSourceUri)
        {
            var referencedSources = (await SymbolTools.GetSources(symbolFilePath)).ToList();

            var srcDir = Path.Combine(GetUnzippedPackageSymbolPath(package), "src");

            var sourceFiles = new HashSet<string>(Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories)
                                .Select(s => s.Substring(srcDir.Length+1)));

            if (referencedSources.Any() && sourceFiles.Any())
            {
                var sourceMapper = new SymbolSourceMapper();
                var mappings = sourceMapper.CreateSourceMappingIndex(package, symbolSourceUri, referencedSources, sourceFiles);

                await SymbolTools.MapSourcesAsync(symbolFilePath, mappings);
                await SymbolTools.IndexSymbolFile(package, symbolFilePath);
            }
        }

        /// <summary>
        /// Returns the path to the folder where the package was decompressed,
        /// or the path to the nupkg file if KeepSourcesCompressed is true
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public string GetPackageSymbolPath(IPackageName package)
        {
            return KeepSourcesCompressed ?
                Path.Combine(SymbolsPath, package.Id + "." + package.Version.ToString() + ".nupkg") :
                GetUnzippedPackageSymbolPath(package);
        }

        /// <summary>
        /// Returns the path a package should be decompressed to (used as the permanent 
        /// storage path or as a temp path if KeepSourcesCompressed is true)
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public string GetUnzippedPackageSymbolPath(IPackageName package)
        {
            return KeepSourcesCompressed ? 
                Path.Combine(SymbolsPath, TempSubFolderName, package.Id + "." + package.Version.ToString()) :
                Path.Combine(SymbolsPath, package.Id, package.Version.ToString());
        }
    }
}