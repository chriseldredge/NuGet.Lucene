using System;

namespace NuGet.Lucene
{
    public class PackageOverwriteDeniedException : Exception
    {
        private readonly IPackageName packageName;

        public PackageOverwriteDeniedException(IPackageName packageName)
            :base(string.Format("Package '{0}' version '{1}' already exists.", packageName.Id, packageName.Version))
        {
            this.packageName = packageName;
        }

        public IPackageName PackageName
        {
            get { return packageName; }
        }
    }
}
