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

        /// <summary>
        /// When <c>true</c>, <see cref="SearchTerm"/> is parsed as a complex boolean query
        /// that can search accross specific fields.
        /// </summary>
        public bool Advanced { get; set; }

        public IEnumerable<string> TargetFrameworks { get; set; }
        public bool AllowPrereleaseVersions { get; set; }
        public PackageOriginFilter PackageOriginFilter { get; set; }
        public SearchSortDirection SortDirection { get; set; }
        public SearchSortField SortField { get; set; }
    }
}
