using System;
using System.Data.Services;

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
            return new Uri(operationContext.AbsoluteServiceUri, string.Format("package/{0}/{1}", package.Id, package.Version));
        }
    }
}