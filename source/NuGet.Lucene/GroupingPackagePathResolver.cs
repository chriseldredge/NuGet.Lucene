namespace NuGet.Lucene
{
    /// <summary>
    /// Groups packages into subfolders based on their id so that
    /// all versions of a package go into the same subfolder.
    /// </summary>
    public class GroupingPackagePathResolver : DefaultPackagePathResolver
    {
        private readonly bool groupPackageFilesById;

        public GroupingPackagePathResolver(string path, bool groupPackageFilesById)
            : base(path, false)
        {
            this.groupPackageFilesById = groupPackageFilesById;
        }

        public override string GetPackageDirectory(string packageId, SemanticVersion version)
        {
            return groupPackageFilesById ? packageId : string.Empty;
        }

        public override string GetPackageFileName(string packageId, SemanticVersion version)
        {
            return packageId + "." + version + Constants.PackageExtension;
        }
    }
}