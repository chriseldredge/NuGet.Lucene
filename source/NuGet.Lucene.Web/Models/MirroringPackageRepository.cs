using System;
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

		public virtual IEnumerable<IPackage> FindPackagesByIdInOrigin(string id)
		{
			try
			{
				return origin.FindPackagesById(id).ToList();
			}
			catch (Exception ex)
			{
				Log.Error(m => m("Exception on FindPackagesById('{0}') for package origin {1}: {2}", id, origin.Source, ex.Message), ex);
				return Enumerable.Empty<IPackage>();
			}
		}

        public virtual IPackage FindPackageInOrigin(string packageId, SemanticVersion version)
        {
	        try
	        {
		        return origin.FindPackage(packageId, version);
	        }
	        catch (Exception ex)
	        {
		        Log.Error(m => m("Exception on FindPackage('{0}', '{1}') for package origin {2}: {3}", packageId, version, origin.Source, ex.Message), ex);
		        return null;
	        }
        }
    }
}