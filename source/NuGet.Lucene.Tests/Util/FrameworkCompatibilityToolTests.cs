using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NuGet.Lucene.Util;
using NUnit.Framework;

namespace NuGet.Lucene.Tests.Util
{
    [TestFixture]
    class FrameworkCompatibilityToolTests
    {
        FrameworkCompatibilityTool tool;

        [SetUp]
        public void SetUp()
        {
            tool = new FrameworkCompatibilityTool();
        }

        [Test]
        public void QueryDisablesCoord()
        {
            var query = tool.GetOrBuildQuery("net45");

            AssertCoordDisabledOnAllQueries(query);
        }

        [Test]
        public void IncludesNonFrameworkPackageQuery()
        {
            var query = tool.GetOrBuildQuery("net20");

            var subquery = query.Clauses.Where(c => ReferenceEquals(c.Query, FrameworkCompatibilityTool.NonFrameworkPackageQuery));

            Assert.That(subquery.Count(), Is.EqualTo(1), "Query \"" + query + "\" should include FrameworkCompatibilityTool.NonFrameworkPackageQuery");
        }

        [Test]
        public void IncludesProjectFramework()
        {
            var query = tool.GetOrBuildQuery("net20");

            var termQueries = GetSupportedFrameworkTermQueries(query);

            Assert.That(termQueries, Is.EquivalentTo(new[] { new TermQuery(new Term(FrameworkCompatibilityTool.SupportedFrameworksFieldName, "net20")) }));
        }

        [Test]
        [TestCase("net20", "net35")]
        [TestCase("net40-client", "net40")]
        public void IncludesKnownCompatibleFramework(string knownFrameworkShortName, string projectFrameworkShortName)
        {
            tool.AddKnownFrameworkShortNames(new[] {knownFrameworkShortName});

            var query = tool.GetOrBuildQuery(projectFrameworkShortName);

            var termQueries = GetSupportedFrameworkTermQueries(query);

            Assert.That(termQueries, Is.EquivalentTo(new[]
                {
                    new TermQuery(new Term(FrameworkCompatibilityTool.SupportedFrameworksFieldName, knownFrameworkShortName)),
                    new TermQuery(new Term(FrameworkCompatibilityTool.SupportedFrameworksFieldName, projectFrameworkShortName))
                }));
        }

        [Test]
        public void CachesQueries()
        {
            var q1 = tool.GetOrBuildQuery("net35");
            var q2 = tool.GetOrBuildQuery("net35");

            Assert.That(q1, Is.SameAs(q2), "Should cache previously computed queries");
        }

        [Test]
        public void AddKnownFrameworkInvalidatesCache()
        {
            var q1 = tool.GetOrBuildQuery("net35");
            tool.AddKnownFrameworkShortNames(new[] {"net20"});
            var q2 = tool.GetOrBuildQuery("net35");

            Assert.That(q1, Is.Not.SameAs(q2), "Cache should be invalidated when adding known framework");
        }

        [Test]
        public void IdemponentAddDoesNotInvalidateCache()
        {
            tool.AddKnownFrameworkShortNames(new[] { "net20", "net35" });
            var q1 = tool.GetOrBuildQuery("net35");
            tool.AddKnownFrameworkShortNames(new[] { "net20" });
            var q2 = tool.GetOrBuildQuery("net35");

            Assert.That(q1, Is.SameAs(q2), "Cache should not be invalidated when not adding new known framework");
        }

        private static TermQuery[] GetSupportedFrameworkTermQueries(BooleanQuery query)
        {
            var subquery =
                (BooleanQuery)
                    query.Clauses.Single(c => !ReferenceEquals(c.Query, FrameworkCompatibilityTool.NonFrameworkPackageQuery))
                        .Query;
            var termQueries = subquery.Clauses.Select(c => c.Query).OfType<TermQuery>().ToArray();
            return termQueries;
        }

        private void AssertCoordDisabledOnAllQueries(BooleanQuery query)
        {
            Assert.That(query.IsCoordDisabled, Is.True, "IsCoordDisabled on query " + query);
            foreach (var childQuery in query.Clauses.Select(c => c.Query).OfType<BooleanQuery>())
            {
                AssertCoordDisabledOnAllQueries(childQuery);
            }
        }
    }
}
