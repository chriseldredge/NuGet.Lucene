using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet.Lucene.Web.Symbols
{
    /// <summary>
    /// Helper to execute Debugging Tools for Windows utilities to update source file paths
    /// and index symbol files.
    /// </summary>
    public class SymbolTools
    {
        public string SymbolPath { get; set; }
        public string ToolPath { get; set; }

        public bool ToolsAvailable
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ToolPath) && File.Exists(GetToolPath("srctool"));
            }
        }

        /// <summary>
        /// Get a list of source files referenced by a PDB symbol file using srctool.exe
        /// </summary>
        public async Task<IEnumerable<string>> GetSources(string symbolFile)
        {
            var output = await ExecuteToolAsync(@"srctool", "-r \"" + symbolFile + "\"");

            // Recent versions of srctool.exe include a mesage like:
            // Foo.pdb: 4 source files are indexed
            // Remove this line when present
            // Also remove blank lines.
            return output.Where(line => line.Trim() != "" && !line.ToLowerInvariant().Contains(symbolFile.ToLowerInvariant()));
        }

        /// <summary>
        /// Uses pdbstr.exe to add a <c>srcsrv</c> stream to a PDB symbol file
        /// that maps source files to an HTTP server.
        /// </summary>
        public async Task MapSourcesAsync(string symbolFile, string sourceMappingIndexContent)
        {
            var indexFile = Path.ChangeExtension(symbolFile, ".index");

            try
            {
                using (var writer = File.CreateText(indexFile))
                {
                    await writer.WriteAsync(sourceMappingIndexContent);
                }

                await ExecuteToolAsync("pdbstr", string.Format(" -w -p:\"{0}\" -i:\"{1}\" -s:srcsrv", symbolFile, indexFile));
            }
            finally
            {
                File.Delete(indexFile);
            }
        }

        /// <summary>
        /// Uses symstore.exe to copy a PDB symbol file to a location
        /// where debuggers will attempt to load it from.
        /// </summary>
        public Task IndexSymbolFile(IPackageName package, string symbolFile)
        {
            return ExecuteToolAsync("symstore", string.Format(" add /f \"{0}\" /s \"{1}\" /t {2} /v {3}", symbolFile, SymbolPath, package.Id, package.Version));
        }

        private async Task<IEnumerable<string>> ExecuteToolAsync(string tool, string arguments)
        {
            if (string.IsNullOrWhiteSpace(ToolPath))
            {
                throw new InvalidOperationException("Cannot process symbol packages without setting path to Debugging Tools for Windows.");
            }

            var exePath = GetToolPath(tool);

            if (!File.Exists(exePath))
            {
                throw new IOException("Cannot locate " + tool + " in " + ToolPath);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(exePath, arguments)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            var tcs = new TaskCompletionSource<int>();

            using (process)
            {
                process.Exited += (s, e) => tcs.SetResult(process.ExitCode);
                process.EnableRaisingEvents = true;
                process.Start();

                var stdout = process.StandardOutput.ReadToEndAsync();

                await Task.WhenAll(tcs.Task, stdout);

                return stdout.Result.Replace("\r\n", "\n").Split('\n');
            }
        }

        private string GetToolPath(string tool)
        {
            var exePath = Path.Combine(ToolPath, tool + ".exe");

            if (!File.Exists(exePath))
            {
                exePath = Path.Combine(Path.Combine(ToolPath, "srcsrv"), tool + ".exe");
            }
            return exePath;
        }
    }
}