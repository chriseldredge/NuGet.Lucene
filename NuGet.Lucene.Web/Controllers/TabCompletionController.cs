using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace NuGet.Lucene.Web.Controllers
{
    public class TabCompletionController : ApiController
    {
        public ILucenePackageRepository Repository { get; set; }

        public IEnumerable<string> GetMatchingPackages(string partialId, bool includePrerelease, int maxResults)
        {
            var packages = GetPackages(includePrerelease);

            if (!string.IsNullOrWhiteSpace(partialId))
            {
                packages = packages.Where(p => p.Id.StartsWith(partialId));
            }

            packages = packages
                .Where(p => p.IsLatestVersion)
                .OrderBy(p => p.Id);

            return packages.Select(p => p.Id).Take(maxResults).ToArray();
        }

        public IEnumerable<string> GetPackageVersions(string packageId, bool includePrerelease)
        {
            var packages = GetPackages(includePrerelease).Where(p => p.Id == packageId);

            return packages.OrderBy(p => p.Version).Select(p => p.Version.ToString()).ToArray();
        }

        private IQueryable<LucenePackage> GetPackages(bool includePrerelease)
        {
            if (!includePrerelease)
            {
                return Repository.LucenePackages.Where(p => !p.IsPrerelease);
            }

            return Repository.LucenePackages;
        }
    }
}