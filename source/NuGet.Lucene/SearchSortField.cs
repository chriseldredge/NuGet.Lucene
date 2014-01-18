namespace NuGet.Lucene
{
    /// <summary>
    /// Specify how to sort results in <see cref="ILucenePackageRepository.Search(NuGet.Lucene.SearchCriteria)"/>.
    /// </summary>
    public enum SearchSortField
    {
        Unspecified = 0,
        Score,
        Id,
        Title,
        Published
    }

    public enum SearchSortDirection
    {
        Ascending,
        Descending
    }
}