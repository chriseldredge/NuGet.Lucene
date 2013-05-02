using System.Collections.Generic;
using System.Linq;
using Common.Logging;

namespace NuGet.Lucene.Web.Models
{
    public interface IMirroringPackageRepository : IPackageLookup, IServiceBasedRepository
    {
        bool MirroringEnabled { get; }
    }

    public class NonMirroringPackageRepository : DelegatingPackageRepository, IMirroringPackageRepository
    {
        public NonMirroringPackageRepository(IPackageRepository target) : base(target)
        {
        }

        public bool MirroringEnabled { get { return false; } }
    }

    public class MirroringPackageRepository : DelegatingPackageRepository, IMirroringPackageRepository
    {
        private static readonly ILog Log = LogManager.GetLogger<MirroringPackageRepository>();

        private readonly IPackageRepository origin;

        public MirroringPackageRepository(IPackageRepository mirror, IPackageRepository origin)
            : base(mirror)
        {
            this.origin = origin;
        }

        public bool MirroringEnabled { get { return true; } }

        public override string Source
        {
            get { return string.Format("{0} auto-target of {1}", base.Source, origin.Source); }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return base.SupportsPrereleasePackages || origin.SupportsPrereleasePackages; }
        }

        public override IEnumerable<IPackage> FindPackagesById(string id)
        {
            var result = base.FindPackagesById(id);

            var remotePackages = FindPackagesByIdInOrigin(id);

            return result.Union(remotePackages, PackageEqualityComparer.IdAndVersion);
        }

        public virtual IEnumerable<IPackage> FindPackagesByIdInOrigin(string id)
        {
            if (origin == null) return Enumerable.Empty<IPackage>();

            return origin.FindPackagesById(id).ToList();
        }

        public override IPackage FindPackage(string packageId, SemanticVersion version)
        {
            var package = base.FindPackage(packageId, version);

            if (package != null) return package;

            package = FindPackageInOrigin(packageId, version);

            if (package == null) return null;

            Log.Info(m => m("Mirroring package {0} {1} from {2}", packageId, version, origin.Source));

            AddPackage(package);

            return base.FindPackage(package.Id, package.Version);
        }

        public virtual IPackage FindPackageInOrigin(string packageId, SemanticVersion version)
        {
            var package = origin.FindPackage(packageId, version);

            DiddlePackage(package);

            return package;
        }

        private void DiddlePackage(IPackage package)
        {
            var dataPackage = package as DataServicePackage;

            if (dataPackage == null) return;
        }
    }
}