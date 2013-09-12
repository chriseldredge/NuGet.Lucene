using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.Lucene.Web.Models
{
    /// <summary>
    /// A package repository base class suitable for decoration and composition.
    /// </summary>
    public class DelegatingPackageRepository : PackageRepositoryBase, IPackageLookup, IServiceBasedRepository
    {
        private readonly IPackageRepository target;

        public DelegatingPackageRepository(IPackageRepository target)
        {
            this.target = target;
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return target.GetPackages();
        }

        public override string Source
        {
            get { return target.Source; }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return target.SupportsPrereleasePackages; }
        }

        public virtual bool Exists(string packageId, SemanticVersion version)
        {
            return target.Exists(packageId, version);
        }

        public virtual IPackage FindPackage(string packageId, SemanticVersion version)
        {
            return target.FindPackage(packageId, version);
        }

        public virtual IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            return target.FindPackagesById(packageId);
        }
        
        public virtual IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            return target.Search(searchTerm, targetFrameworks, allowPrereleaseVersions);
        }

        IEnumerable<IPackage> IServiceBasedRepository.GetUpdates(IEnumerable<IPackage> packages, bool includePrerelease, bool includeAllVersions,
            IEnumerable<FrameworkName> targetFrameworks, IEnumerable<IVersionSpec> versionConstraints)
        {
            return target.GetUpdates(packages, includePrerelease, includeAllVersions, targetFrameworks, versionConstraints);
        }

        public override void AddPackage(IPackage package)
        {
            target.AddPackage(package);
        }

        public override void RemovePackage(IPackage package)
        {
            target.RemovePackage(package);
        }

    }
}