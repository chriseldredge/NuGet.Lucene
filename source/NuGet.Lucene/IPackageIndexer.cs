using System;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Lucene
{
    public interface IPackageIndexer
    {
        /// <summary>
        /// Gets status of index building activity.
        /// </summary>
        IndexingStatus GetIndexingStatus();

        /// <summary>
        /// Raised whenever status changes.
        /// </summary>
        IObservable<IndexingStatus> StatusChanged { get; }

        Task SynchronizeIndexWithFileSystemAsync(SynchronizationMode mode, CancellationToken cancellationToken);
        Task AddPackageAsync(LucenePackage package, CancellationToken cancellationToken);
        Task RemovePackageAsync(IPackage package, CancellationToken cancellationToken);
        Task IncrementDownloadCountAsync(IPackage package, CancellationToken cancellationToken);

        void Optimize();
    }
}
