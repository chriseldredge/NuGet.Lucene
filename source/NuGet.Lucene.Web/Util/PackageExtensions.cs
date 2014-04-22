using System;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Util
{
    public static class PackageExtensions
    {
        public static ODataPackage ToODataPackage(this IPackage package)
        {
            var lucenePackage = package as LucenePackage;

            if (lucenePackage != null)
                return new ODataPackage(lucenePackage);

            var dataServicePackage = package as NuGet.DataServicePackage;

            if (dataServicePackage != null)
                return new ODataPackage(dataServicePackage);

            throw new ArgumentException("Cannot convert package of type " + package.GetType() + " to ODataPackage.");
        }

    }
}