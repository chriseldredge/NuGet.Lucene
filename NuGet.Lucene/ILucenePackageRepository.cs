using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Lucene
{
    public interface ILucenePackageRepository : IServiceBasedRepository, IPackageLookup
    {
        /// <summary>
        /// Async implementation of <see cref="IPackageRepository.AddPackage"/>.
        /// </summary>
        Task AddPackageAsync(IPackage package);

        /// <summary>
        /// Async implementation of <see cref="IPackageRepository.RemovePackage"/>.
        /// </summary>
        Task RemovePackageAsync(IPackage package);

        /// <summary>
        /// Scans the packages directory and compares it with
        /// packages in the Lucene index, then applies changes
        /// to the Lucene index to synchronize it.
        /// </summary>
        Task SynchronizeWithFileSystem(CancellationToken cancellationToken);

        /// <summary>
        /// Gets an object that contains information about
        /// current indexing activity.
        /// </summary>
        IndexingStatus GetIndexingStatus();

        IObservable<IndexingStatus> StatusChanged { get; }
        
        /// <summary>
        /// Loads pacakge data from the Lucene index with a given path.
        /// </summary>
        LucenePackage LoadFromIndex(string path);

        /// <summary>
        /// Loads a package from disk with a given path, then
        /// converts it to a <c cref="LucenePackage"/> using <c cref="Convert"/>.
        /// </summary>
        LucenePackage LoadFromFileSystem(string path);

        /// <summary>
        /// Converts a given generic <c cref="IPackage"/> and returns
        /// a <c cref="LucenePackage"/> that holds additional information
        /// such as package hash, size, etc.
        /// </summary>
        LucenePackage Convert(IPackage package);

        /// <summary>
        /// Provides access to more strongly typed packages to facilitate
        /// querying on properties not found on <see cref="IPackage"/>.
        /// </summary>
        IQueryable<LucenePackage> LucenePackages { get; }

        /// <summary>
        /// Increments the <see cref="LucenePackage.VersionDownloadCount"/> for 
        /// this package and the <see cref="LucenePackage.DownloadCount"/> for
        /// all packages with the same <see cref="LucenePackage.Id"/>.
        /// </summary>
        Task IncrementDownloadCount(IPackage package);
    }
}