using System;

namespace NuGet.Lucene
{
    public enum IndexingState
    {
        Idle,
        Updating,
        Committing,
        Optimizing,
    }

    public enum SynchronizationState
    {
        Idle, Scanning, Building
    }

    public class SynchronizationStatus
    {
        public readonly SynchronizationState SynchronizationState;
        public readonly string CurrentPackagePath;
        public readonly int CompletedPackages;
        public readonly int PackagesToIndex;

        public SynchronizationStatus(SynchronizationState synchronizationState, string currentPackagePath, int completedPackages, int packagesToIndex)
        {
            SynchronizationState = synchronizationState;
            CurrentPackagePath = currentPackagePath;
            CompletedPackages = completedPackages;
            PackagesToIndex = packagesToIndex;
        }

        public SynchronizationStatus(SynchronizationState synchronizationState)
            : this(synchronizationState, string.Empty, 0, 0)
        {
        }
    }

    public class IndexingStatus
    {
        public readonly IndexingState State;
        public readonly SynchronizationStatus SynchronizationStatus;
        public readonly int TotalPackages;
        public readonly int PendingDeletes;
        public readonly bool IsOptimized;
        public readonly DateTime LastModification;

        public IndexingStatus(IndexingState state, SynchronizationStatus synchronizationStatus, int totalPackages, int pendingDeletes, bool isOptimized, DateTime lastModification)
        {
            State = state;
            SynchronizationStatus = synchronizationStatus;
            TotalPackages = totalPackages;
            PendingDeletes = pendingDeletes;
            IsOptimized = isOptimized;
            LastModification = lastModification;
        }
    }
}