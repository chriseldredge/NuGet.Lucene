using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Lucene.Net.Index;
using Lucene.Net.Linq;
using Lucene.Net.Search;

namespace NuGet.Lucene.Util
{
    /// <summary>
    /// Uses a list of supported frameworks of all packages currently in the index
    /// to filter search results by a target framework that will only return packages
    /// that are compatible or do not target any frameworks.
    /// </summary>
    public class FrameworkCompatibilityTool
    {
        internal const string SupportedFrameworksFieldName = "SupportedFrameworks";

        private volatile ISet<FrameworkName> knownFrameworkNames = new HashSet<FrameworkName>();
        private readonly ConcurrentDictionary<string, BooleanQuery> queryCache = new ConcurrentDictionary<string, BooleanQuery>(StringComparer.OrdinalIgnoreCase);

        private readonly object sync = new object();

        internal static readonly BooleanQuery NonFrameworkPackageQuery = new BooleanQuery(disableCoord:true)
        {
            {new MatchAllDocsQuery(), Occur.MUST},
            {new WildcardQuery(new Term(SupportedFrameworksFieldName, "*")), Occur.MUST_NOT}
        };

        public void InitializeKnownFrameworkShortNamesFromIndex(LuceneDataProvider provider)
        {
            AddKnownFrameworkShortNames(GetKnownFrameworkShortNamesFromIndex(provider));
        }

        public IEnumerable<string> GetKnownFrameworkShortNamesFromIndex(LuceneDataProvider provider)
        {
            var termEnum = provider.IndexWriter.GetReader().Terms();
            while (termEnum.Next())
            {
                if (termEnum.Term.Field == SupportedFrameworksFieldName)
                {
                    yield return termEnum.Term.Text;
                }
            }
        }

        public void AddKnownFrameworkShortNames(IEnumerable<string> items)
        {
            var newItems = items
                .Select(VersionUtility.ParseFrameworkName)
                .Where(f => f != VersionUtility.UnsupportedFrameworkName)
                .Except(knownFrameworkNames)
                .ToArray();

            if (newItems.Length == 0) return;

            lock (sync)
            {
                knownFrameworkNames = new HashSet<FrameworkName>(knownFrameworkNames.Union(newItems));
            }

            queryCache.Clear();
        }

        public IQueryable<LucenePackage> FilterByTargetFramework(IQueryable<LucenePackage> packages, string projectFrameworkShortName)
        {
            return packages.Where(GetOrBuildQuery(projectFrameworkShortName));
        }

        public BooleanQuery GetOrBuildQuery(string projectFrameworkShortName)
        {
            return queryCache.GetOrAdd(projectFrameworkShortName, BuildQuery);
        }

        private BooleanQuery BuildQuery(string projectFrameworkShortName)
        {
            var projectFrameworkName = VersionUtility.ParseFrameworkName(projectFrameworkShortName);

            string[] searchFrameworks;

            if (projectFrameworkName == VersionUtility.UnsupportedFrameworkName)
            {
                searchFrameworks = new[] {projectFrameworkShortName.ToLowerInvariant()};
            }
            else
            {
                searchFrameworks = knownFrameworkNames
                    .Union(new[] {projectFrameworkName})
                    .Where(candidate => VersionUtility.IsCompatible(projectFrameworkName, new[] {candidate}))
                    .Select(VersionUtility.GetShortFrameworkName)
                    .Select(s => s.ToLowerInvariant())
                    .ToArray();
            }

            var query = new BooleanQuery(disableCoord: true);

            foreach (var framework in searchFrameworks)
            {
                query.Add(new TermQuery(new Term(SupportedFrameworksFieldName, framework)), Occur.SHOULD);
            }

            return new BooleanQuery(disableCoord:true)
            {
                {query, Occur.SHOULD},
                {NonFrameworkPackageQuery, Occur.SHOULD}
            };
        }
    }
}
