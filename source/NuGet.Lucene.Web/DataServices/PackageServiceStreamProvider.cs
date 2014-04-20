/*
using System;
using System.Data.Services;
using System.IO;
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
            var package = (ODataPackage)entity;
            
            var vpath = GetPackageDownloadPath(package);

            return new Uri(operationContext.AbsoluteRequestUri, vpath);
        }

        public virtual string GetPackageDownloadPath(ODataPackage package)
        {
            var route = RouteTable.Routes[RouteNames.Packages.Download];

            var routeValues = new {id = package.Id, version = package.Version, httproute = true};
            
            var path = route.GetVirtualPath(RequestContext, new RouteValueDictionary(routeValues)).VirtualPath;
            return VirtualPathUtility.ToAbsolute("~/" + path);
        }

        public static RequestContext RequestContext
        {
            get
            {
                var httpContext = HttpContext.Current;
                var request = new EmptyInputStreamHttpRequestWrapper(httpContext.Request);
                return new RequestContext(new HttpContextWrapperWithRequest(httpContext, request), new RouteData());
            }
        }
    }

    /// <summary>
    /// Allow HttpContext.Request to be replaced with an arbitrary HttpRequestBase instance.
    /// </summary>
    class HttpContextWrapperWithRequest : HttpContextWrapper
    {
        private readonly HttpRequestBase request;

        public HttpContextWrapperWithRequest(HttpContext httpContext, HttpRequestBase request) : base(httpContext)
        {
            this.request = request;
        }

        public override HttpRequestBase Request
        {
            get
            {
                return request;
            }
        }
    }

    /// <summary>
    /// Prevents "System.Web.HttpException (0x80004005): This method or property is not
    /// supported after HttpRequest.GetBufferlessInputStream has been invoked." from being
    /// thrown at System.Web.HttpRequest.get_InputStream().
    /// </summary>
    class EmptyInputStreamHttpRequestWrapper : HttpRequestWrapper
    {
        public EmptyInputStreamHttpRequestWrapper(HttpRequest httpRequest) : base(httpRequest)
        {
        }

        public override Stream InputStream
        {
            get
            {
                return new MemoryStream();
            }
        }
    }
}
*/