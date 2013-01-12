using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NuGet.Lucene
{
    public class IndexDifferenceCalculator
    {
        private readonly IFileSystem fileSystem;
        private readonly string[] fileSystemPackages;
        private readonly Dictionary<string, LucenePackage> indexedPackagesByPath;

        private IndexDifferenceCalculator(IFileSystem fileSystem, string[] fileSystemPackages, Dictionary<string, LucenePackage> indexedPackagesByPath)
        {
            this.fileSystem = fileSystem;
            this.fileSystemPackages = fileSystemPackages;
            this.indexedPackagesByPath = indexedPackagesByPath;
        }

        public static IndexDifferences FindDifferences(IFileSystem fileSystem, IEnumerable<LucenePackage> indexedPackages, CancellationToken cancellationToken)
        {
            var fileSystemPackages = fileSystem.GetFiles(string.Empty, "*" + Constants.PackageExtension, true)
                                               .AsParallel()
                                               .WithDegreeOfParallelism(2)
                                               .WithCancellation(cancellationToken)
                                               .ToArray();

            var indexedPackagesByPath = indexedPackages.AsParallel()
                                                       .WithCancellation(cancellationToken)
                                                       .ToDictionary(pkg => pkg.Path, StringComparer.InvariantCultureIgnoreCase);
            
            var calc = new IndexDifferenceCalculator(fileSystem, fileSystemPackages, indexedPackagesByPath);

            return calc.Calculate();
        }

        private IndexDifferences Calculate()
        {
            var newPackages = fileSystemPackages.Except(indexedPackagesByPath.Keys, StringComparer.InvariantCultureIgnoreCase);
            var missingPackages = indexedPackagesByPath.Keys.Except(fileSystemPackages, StringComparer.InvariantCultureIgnoreCase);
            var modifiedPackages = fileSystemPackages.Intersect(indexedPackagesByPath.Keys, StringComparer.InvariantCultureIgnoreCase).Where(ModifiedDateMismatch);

            return new IndexDifferences(newPackages.ToList(), missingPackages.ToList(), modifiedPackages.ToList());
        }

        private bool ModifiedDateMismatch(string path)
        {
            var lucenePackage = indexedPackagesByPath[path];

            if (!lucenePackage.Published.HasValue)
            {
                return true;
            }

            return TimestampsMismatch(lucenePackage, fileSystem.GetLastModified(path));
        }

        internal static bool TimestampsMismatch(LucenePackage lucenePackage, DateTimeOffset fileLastModified)
        {
            var diff = fileLastModified - lucenePackage.Published.GetValueOrDefault(DateTimeOffset.MinValue);
            return Math.Abs(diff.TotalSeconds) > 1;
        }
    }
}