using System.Linq;
using System.Web.Mvc;
using Ninject;

namespace NuGet.Lucene.Web.Controllers
{
    public class TabCompletionController : Controller
    {
        [Inject]
        public ILucenePackageRepository Repository { get; set; }

        public JsonResult GetMatchingPackages(string partialId, bool? includePrerelease, int maxResults)
        {
            var packages = GetPackages(includePrerelease);

            if (!string.IsNullOrWhiteSpace(partialId))
            {
                packages = packages.Where(p => p.Id.StartsWith(partialId));
            }

            packages = packages
                .Where(p => p.IsLatestVersion)
                .OrderBy(p => p.Id);

            var data = packages.Select(p => p.Id).Take(maxResults).ToArray();

            return JsonResult(data);
        }

        public JsonResult GetPackageVersions(string packageId, bool? includePrerelease)
        {
            var packages = GetPackages(includePrerelease).Where(p => p.Id == packageId);

            var data = packages.OrderBy(p => p.Version).Select(p => p.Version.ToString()).ToArray();

            return JsonResult(data);
        }

        private IQueryable<LucenePackage> GetPackages(bool? includePrerelease)
        {
            if (!includePrerelease.GetValueOrDefault(false))
            {
                return Repository.LucenePackages.Where(p => !p.IsPrerelease);
            }

            return Repository.LucenePackages;
        }

        private static JsonResult JsonResult(object data)
        {
            return new JsonResult {Data = data, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
        }
    }
}