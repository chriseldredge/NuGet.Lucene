namespace NuGet.Lucene
{
    public class RepositoryInfo
    {
        private readonly IndexingStatus indexingStatus;
        public readonly int TotalPackages;
        
        public RepositoryInfo(int totalPackages, IndexingStatus indexingStatus)
        {
            TotalPackages = totalPackages;
            this.indexingStatus = indexingStatus;
        }

        public IndexingState IndexingState { get { return indexingStatus.State; } }
        public SynchronizationState SynchronizationState { get { return indexingStatus.SynchronizationStatus.SynchronizationState; } }
        public int PackagesToIndex { get { return indexingStatus.SynchronizationStatus.PackagesToIndex; } }
        public int CompletedPackages { get { return indexingStatus.SynchronizationStatus.CompletedPackages; } }
    }

    public enum IndexingState
    {
        Idle,
        Updating,
        Committing,
        Optimizing,
    }

    public enum SynchronizationState
    {
        Idle, ScanningFiles, ScanningIndex, Comparing, Indexing
    }

    public class SynchronizationStatus
    {
        public readonly SynchronizationState SynchronizationState;
        public readonly int CompletedPackages;
        public readonly int PackagesToIndex;

        public SynchronizationStatus(SynchronizationState synchronizationState, int completedPackages, int packagesToIndex)
        {
            SynchronizationState = synchronizationState;
            CompletedPackages = completedPackages;
            PackagesToIndex = packagesToIndex;
        }

        public SynchronizationStatus(SynchronizationState synchronizationState)
            : this(synchronizationState, 0, 0)
        {
        }
    }

    public class IndexingStatus
    {
        public readonly IndexingState State;
        public readonly SynchronizationStatus SynchronizationStatus;

        public IndexingStatus(IndexingState state, SynchronizationStatus synchronizationStatus)
        {
            State = state;
            SynchronizationStatus = synchronizationStatus;
        }
    }
}