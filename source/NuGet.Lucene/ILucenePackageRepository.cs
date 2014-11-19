using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Lucene.IO;

namespace NuGet.Lucene
{
    public interface ILucenePackageRepository : IServiceBasedRepository, IPackageLookup
    {
        /// <summary>
        /// Async implementation of <see cref="IPackageRepository.AddPackage"/>.
        /// </summary>
        Task AddPackageAsync(IPackage package, CancellationToken cancellationToken);

        /// <summary>
        /// Async implementation of <see cref="IPackageRepository.RemovePackage"/>.
        /// </summary>
        Task RemovePackageAsync(IPackage package, CancellationToken cancellationToken);

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
        RepositoryInfo GetStatus();

        IObservable<RepositoryInfo> StatusChanged { get; }
        
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
        Task IncrementDownloadCountAsync(IPackage package, CancellationToken cancellationToken);

        /// <summary>
        /// Overload of <see cref="IServiceBasedRepository.Search"/>
        /// using parameter object for better flexibility.
        /// </summary>
        /// <exception cref="InvalidSearchCriteriaException">
        /// When <see cref="SearchCriteria.SearchTerm"/> cannot be parsed.
        /// </exception>
        IQueryable<IPackage> Search(SearchCriteria criteria);

        /// <summary>
        /// Get an enumeration of fields that can be queried using native Lucene queries.
        /// </summary>
        IEnumerable<string> GetAvailableSearchFieldNames();

        /// <summary>
        /// Requests that the Lucene index be optimized, forcing all segments
        /// to be merged and expunging deleted documents.
        /// </summary>
        void Optimize();

        /// <summary>
        /// Creates a stream appropriate for staging a package that will be added
        /// after the contents are written into place.
        /// 
        /// The stream returned automatically calculates a hash of the package contents
        /// while the stream is being written to avoid additional I/O.
        /// </summary>
        HashingWriteStream CreateStreamForStagingPackage();

        /// <summary>
        /// Loads a staged package (after the stream has been written).
        /// </summary>
        IFastZipPackage LoadStagedPackage(HashingWriteStream packageStream);

        /// <summary>
        /// Deletes a temporary package that won't be added for whatever reason.
        /// </summary>
        void DiscardStagedPackage(HashingWriteStream packageStream);
    }
}
