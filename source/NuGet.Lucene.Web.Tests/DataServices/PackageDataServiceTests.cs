using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Moq;
using NUnit.Framework;
using NuGet.Lucene.Web.DataServices;

namespace NuGet.Lucene.Web.Tests.DataServices
{
    [TestFixture]
    public class PackageDataServiceTests
    {
        private TestablePackageDataService service;
        private Mock<ILucenePackageRepository> repo;

        [SetUp]
        public void SetUp()
        {
            repo = new Mock<ILucenePackageRepository>();
            service = new TestablePackageDataService
                {
                    PackageRepository = repo.Object,
                    FakeRequestUri = new Uri("http://localhost/packages?searchTerm=foo")
                };
        }

        [Test]
        public void SearchSortsByScore()
        {
            var packages = new LucenePackage[0];

            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable());

            var query = service.Search("foo", "", includePrerelease: false);

            AssertOrderingBy(query, "result => result.Score()");
        }

        [Test]
        public void SearchNoSortWhenSpecifiedInQueryString()
        {
            service.FakeRequestUri = new Uri("http://localhost/packages?$orderby=DownloadCount");
            var packages = new LucenePackage[0];

            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable());

            var query = service.Search("foo", "", includePrerelease: false);

            AssertOrderingBy(query);
        }

        private static void AssertOrderingBy(IQueryable<Web.DataServices.DataServicePackage> query, params string[] expectedOrdering)
        {
            var finder = new OrderingClauseFinder();

            finder.Visit(query.Expression);

            Assert.That(finder.Matches.Count(), Is.EqualTo(expectedOrdering.Length), "ordering expressions");
            var expressions = finder.Matches.Select(m => m.Arguments.Last().ToString()).ToArray();
            Assert.That(expressions, Is.EqualTo(expectedOrdering), "ordering expression");
        }

        class TestablePackageDataService : PackageDataService
        {
            public Uri FakeRequestUri { get; set; }

            protected override System.Uri CurrentRequestUri
            {
                get
                {
                    return FakeRequestUri;
                }
            }
        }

        class OrderingClauseFinder : ExpressionVisitor
        {
            private static readonly string[] methodNames = new[] {"OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending"};
            private readonly List<MethodCallExpression> matches = new List<MethodCallExpression>();

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (methodNames.Contains(node.Method.Name))
                {
                    matches.Add(node);
                }
                return base.VisitMethodCall(node);
            }

            public IEnumerable<MethodCallExpression> Matches {get { return matches; } }
        }
    }
}
