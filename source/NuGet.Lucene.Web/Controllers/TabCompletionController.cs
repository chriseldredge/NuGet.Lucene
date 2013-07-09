using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// Actions that enable fast tab-completion from the Package Manager Console in Visual Studio. The interface for
    /// these actions is documented at <a href="https://github.com/NuGet/NuGetGallery/wiki/Tab-Completion-API-Endpoints">https://github.com/NuGet/NuGetGallery/wiki/Tab-Completion-API-Endpoints</a>.
    /// </summary>
    public class TabCompletionController : ApiController
    {
        public ILucenePackageRepository Repository { get; set; }

        /// <summary>
        /// Find packages that start with <paramref name="partialId"/>.
        /// </summary>
        /// <param name="partialId">The pattern to match (case insensitive)</param>
        /// <param name="includePrerelease">Flag indicating if pre-release packages should be returned (false by default)</param>
        /// <param name="maxResults">Maximum results to return</param>
        public IEnumerable<string> GetMatchingPackages(string partialId, bool includePrerelease = false, int maxResults = 30)
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

        /// <summary>
        /// Finds versions of a given package.
        /// </summary>
        /// <param name="packageId">The complete package id</param>
        /// <param name="includePrerelease">Flag indicating if pre-release package versions should be included (false by default)</param>
        /// <returns>Set of available versions that match</returns>
        public IEnumerable<string> GetPackageVersions(string packageId, bool includePrerelease = false)
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