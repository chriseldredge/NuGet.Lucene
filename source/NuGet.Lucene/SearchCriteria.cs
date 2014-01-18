using System.Collections.Generic;

namespace NuGet.Lucene
{
    public class SearchCriteria
    {
        public SearchCriteria(string searchTerm)
        {
            SearchTerm = searchTerm;
        }

        public string SearchTerm { get; set; }
        public IEnumerable<string> TargetFrameworks { get; set; }
        public bool AllowPrereleaseVersions { get; set; }
        public PackageOriginFilter PackageOriginFilter { get; set; }
        public SearchSortDirection SortDirection { get; set; } 
        public SearchSortField SortField { get; set; }
    }
}