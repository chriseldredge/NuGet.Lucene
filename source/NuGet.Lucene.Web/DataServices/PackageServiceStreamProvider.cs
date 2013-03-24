using System;
using System.Data.Services;
using System.Web;
using System.Web.Routing;

namespace NuGet.Lucene.Web.DataServices
{
    public class PackageServiceStreamProvider : DefaultServiceStreamProvider
    {
        public PackageServiceStreamProvider()
        {
            ContentType = "application/zip";
        }

        public override Uri GetReadStreamUri(object entity, DataServiceOperationContext operationContext)
        {
            var package = (DataServicePackage)entity;
            
            var vpath = GetPackageDownloadPath(package);

            return new Uri(operationContext.AbsoluteRequestUri, vpath);
        }

        public string GetPackageDownloadPath(DataServicePackage package)
        {
            var route = RouteTable.Routes[RouteNames.Packages.Download];

            var routeValues = new {id = package.Id, version = package.Version, httproute = true};
            
            var path = route.GetVirtualPath(RequestContext, new RouteValueDictionary(routeValues)).VirtualPath;
            return VirtualPathUtility.ToAbsolute("~/" + path);
        }

        private static RequestContext RequestContext
        {
            get { return new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()); }
        }
    }
}