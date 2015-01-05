namespace NuGet.Lucene
{
    public enum SynchronizationMode
    {
        /// <summary>
        /// The default method. Compares packages in the file system with
        /// the Lucene index and only makes changes where differences are
        /// detected.
        /// </summary>
        Incremental,

        /// <summary>
        /// Completely rebuilds the Lucene index from scratch. This method
        /// is much less efficient for large repositories but may be necessary
        /// when upgrading NuGet.Lucene to a new version or when differences
        /// are not correctly detected by comparing the last-modified date
        /// of package files with index metadata.
        /// </summary>
        Complete
    }
}
