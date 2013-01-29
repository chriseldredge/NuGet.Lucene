using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Ninject;
using NuGet.Lucene.Web.Models;
using NuGet.Lucene.Web.Mvc;

namespace NuGet.Lucene.Web.Controllers
{
    public class PackagesController : Controller
    {
        [Inject]
        public ILucenePackageRepository Repository { get; set; }

        public ActionResult Status()
        {
            return View(Repository.GetIndexingStatus());
        }

        // TODO: send Last-Modified / Etag, handle If-Modified-Since header
        public ActionResult Download(PackageSpec packageSpec)
        {
            LucenePackage package;

            if (packageSpec.Version != null)
            {
                package = (LucenePackage)Repository.FindPackage(packageSpec.Id, packageSpec.Version);
            }
            else
            {
                package = (LucenePackage)Repository.FindPackagesById(packageSpec.Id).OrderBy(p => p.Version).LastOrDefault();
            }

            if (package == null)
            {
                return new HttpNotFoundResult();
            }

            if (string.Equals(Request.HttpMethod, "GET", StringComparison.CurrentCultureIgnoreCase))
            {
                // Don't wait for Task to complete so request will process without blocking.
                Repository.IncrementDownloadCount(package);
            }

            var filename = string.Format("{0}.{1}{2}", package.Id, package.Version, Constants.PackageExtension);
            return new HeadSupportingFileStreamResult(package.GetStream(), "application/zip")
                {
                    FileDownloadName = filename,
                    ContentLength = package.PackageSize,
                    LastModified = package.LastUpdated
                };
        }

        [AcceptVerbs("DELETE")]
        public async Task<ActionResult> Delete(PackageSpec packageSpec)
        {
            var package = Repository.FindPackage(packageSpec.Id, packageSpec.Version);

            if (package == null)
            {
                return new HttpNotFoundResult(string.Format("Package {0} version {1} not found.", packageSpec.Id, packageSpec.Version));
            }

            await Repository.RemovePackageAsync(package);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [AcceptVerbs("PUT", "POST")]
        public async Task<ActionResult> Upload([ModelBinder(typeof(ZipPackageUploadModelBinder))]ZipPackage package)
        {
            await Repository.AddPackageAsync(package);

            // Not sure if there is a programmatic way to generate this URL from WCF Data Services
            var location = string.Format("~/api/v2/Packages(Id='{0}',Version='{1}')", package.Id, package.Version);

            return new HttpStatusCreatedResult(location);
        }
    }
}