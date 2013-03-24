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

        Task SynchronizeIndexWithFileSystem(CancellationToken cancellationToken);
        Task AddPackage(LucenePackage package);
        Task RemovePackage(IPackage package);
        Task IncrementDownloadCount(IPackage package);

        void Optimize();
    }
}