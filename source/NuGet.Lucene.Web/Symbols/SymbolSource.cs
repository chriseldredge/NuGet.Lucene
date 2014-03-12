using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task AddSymbolsAsync(IPackage package, string symbolSourceUri)
        {
            await UnzipPackageAsync(package);
            await ProcessSymbolsAsync(package, symbolSourceUri);
        }
        
        public Task RemoveSymbolsAsync(IPackageName package)
        {
            var path = GetPackageSymbolPath(package);

            Directory.Delete(path, recursive:true);

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
            var parts = new[]
            {
                package.Id,
                package.Version.ToString(),
                "src",
                relativePath
            };

            return OpenFile(Path.Combine(parts));
        }

        public async Task UnzipPackageAsync(IPackage package)
        {
            var dir = GetPackageSymbolPath(package);

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
            var packageSymbolsDir = Path.Combine(GetPackageSymbolPath(package), "lib");
            var files = Directory.EnumerateFiles(packageSymbolsDir, "*.pdb", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                await ProcessSymbolFileAsync(package, file, symbolSourceUri);
            }
        }

        public async Task ProcessSymbolFileAsync(IPackageName package, string symbolFilePath, string symbolSourceUri)
        {
            var referencedSources = (await SymbolTools.GetSources(symbolFilePath)).ToList();

            var srcDir = Path.Combine(GetPackageSymbolPath(package), "src");

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

        public string GetPackageSymbolPath(IPackageName package)
        {
            return Path.Combine(SymbolsPath, package.Id, package.Version.ToString());
        }
    }
}