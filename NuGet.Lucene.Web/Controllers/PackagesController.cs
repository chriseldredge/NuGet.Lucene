using System;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Http;
using NuGet.Lucene.Web.Models;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Controllers
{
    public class PackagesController : ApiController
    {
        public ILucenePackageRepository Repository { get; set; }

        public dynamic GetPackageInfo([FromUri]PackageSpec packageSpec)
        {
            var packages = Repository
                            .LucenePackages
                            .Where(p => p.Id == packageSpec.Id)
                            .OrderBy(p => p.Version)
                            .ToList();

            var package = packageSpec.Version != null
                              ? packages.Find(p => p.Version.SemanticVersion == packageSpec.Version)
                              : packages.LastOrDefault();
            
            if (package == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Package not found.");
            }

            var versionHistory = packages.Select(
                pkg => new
                    {
                        pkg.Version,
                        pkg.LastUpdated,
                        pkg.VersionDownloadCount,
                        Link = Url.Link(RouteNames.PackageInfo, new { id = pkg.Id, version = pkg.Version })
                    });

            dynamic result = new ExpandoObject();
            result.Package = package;
            result.VersionHistory = versionHistory.ToArray();
            return result;
        }

        [HttpGet]
        [HttpHead]
        public HttpResponseMessage DownloadPackage([FromUri]PackageSpec packageSpec)
        {
            var package = FindPackage(packageSpec);

            var result = EvaluateCacheHeaders(packageSpec, package);

            if (result != null)
            {
                return result;
            }

            result = Request.CreateResponse(HttpStatusCode.OK);

            if (Request.Method == HttpMethod.Get)
            {
                result.Content = new StreamContent(package.GetStream());
                
                TaskUtils.FireAndForget(() => Repository.IncrementDownloadCount(package), UnhandledExceptionLogger.LogException);
            }
            else
            {
                result.Content = new StringContent(string.Empty);
            }

            result.Headers.ETag = new EntityTagHeaderValue('"' + package.PackageHash + '"');
            result.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/zip");
            result.Content.Headers.LastModified = package.LastUpdated;
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
                {
                    FileName = string.Format("{0}.{1}{2}", package.Id, package.Version, Constants.PackageExtension),
                    Size = package.PackageSize,
                    CreationDate = package.Created,
                    ModificationDate = package.LastUpdated,
                };
            
            return result;
        }

        private HttpResponseMessage EvaluateCacheHeaders(PackageSpec packageSpec, LucenePackage package)
        {
            if (package == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound,
                                                     string.Format("Package {0} version {1} not found.", packageSpec.Id,
                                                                   packageSpec.Version));
            }

            var etagMatch = Request.Headers.IfMatch.Any(etag => !etag.IsWeak && etag.Tag == '"' + package.PackageHash + '"');
            var notModifiedSince = Request.Headers.IfModifiedSince.HasValue &&
                                   Request.Headers.IfModifiedSince >= package.LastUpdated;

            if (etagMatch || notModifiedSince)
            {
                return Request.CreateResponse(HttpStatusCode.NotModified);
            }

            return null;
        }

        [HttpGet]
        public dynamic Search(string query = "", bool includePrerelease = false, int offset = 0, int count = 20)
        {
            var queryable = Repository.Search(query, new string[0], includePrerelease).Where(p => p.IsLatestVersion);
            var totalHits = queryable.Count();
            var hits = queryable.Skip(offset).Take(count).ToList();

            dynamic result = new ExpandoObject();
            result.Query = query;
            result.IncludePrerelease = includePrerelease;
            result.TotalHits = totalHits;
            result.Hits = hits;
            result.Offset = offset;
            return result;
        }

        public async Task<HttpResponseMessage> DeletePackage([FromUri]PackageSpec packageSpec)
        {
            if (packageSpec == null || string.IsNullOrWhiteSpace(packageSpec.Id) || packageSpec.Version == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify package id and version.");
            }

            var package = Repository.FindPackage(packageSpec.Id, packageSpec.Version);

            if (package == null)
            {
                var message = string.Format("Package {0} version {1} not found.", packageSpec.Id, packageSpec.Version);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, message);
            }

            await Repository.RemovePackageAsync(package);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPut]
        [HttpPost]
        public async Task<HttpResponseMessage> PutPackage(IPackage package)
        {
            if (package == null || string.IsNullOrWhiteSpace(package.Id) || package.Version == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must provide package with valid id and version.");
            }

            await Repository.AddPackageAsync(package);

            var location = Url.Link(RouteNames.PackageInfo, new { id = package.Id, version = package.Version });

            var result = Request.CreateResponse(HttpStatusCode.Created);
            result.Headers.Location = new Uri(location);
            return result;
        }

        private LucenePackage FindPackage(PackageSpec packageSpec)
        {
            LucenePackage package;

            if (packageSpec.Version != null)
            {
                package = (LucenePackage)Repository.FindPackage(packageSpec.Id, packageSpec.Version);
            }
            else
            {
                package = Repository.FindPackagesById(packageSpec.Id)
                                    .Cast<LucenePackage>()
                                    .Where(p => p.IsPrerelease == false)
                                    .OrderBy(p => p.Version).LastOrDefault();
            }
            return package;
        }
    }
}