using System.Linq;

namespace NuGet.Lucene.Util
{
    public static class PackageQueryableExtensions
    {
        public static IQueryable<LucenePackage> LatestOnly(this IQueryable<LucenePackage> queryable, bool includePrerelease)
        {
            if (includePrerelease)
            {
                return queryable.Where(p => p.IsAbsoluteLatestVersion);
            }

            return queryable.Where(p => p.IsLatestVersion);
        }

        public static IQueryable<IPackage> LatestOnly(this IQueryable<IPackage> queryable, bool includePrerelease)
        {
            if (includePrerelease)
            {
                return queryable.Where(p => p.IsAbsoluteLatestVersion);
            }

            return queryable.Where(p => p.IsLatestVersion);
        }
    }
}