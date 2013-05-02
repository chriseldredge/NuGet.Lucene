using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Moq;
using NUnit.Framework;
using NuGet.Lucene.Web.DataServices;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Tests.DataServices
{
    public abstract class PackageDataServiceTestBase
    {
        protected TestablePackageDataService service;
        protected Mock<IMirroringPackageRepository> repo;

        [SetUp]
        public void SetUp()
        {
            repo = new Mock<IMirroringPackageRepository>();
            service = new TestablePackageDataService
                {
                    PackageRepository = repo.Object,
                    FakeRequestUri = new Uri("http://localhost/packages?searchTerm=foo")
                };
        }

        protected static void AssertOrderingBy(IQueryable<Web.DataServices.DataServicePackage> query, params string[] expectedOrdering)
        {
            var finder = new OrderingClauseFinder();

            finder.Visit(query.Expression);

            Assert.That(Enumerable.Count(finder.Matches), Is.EqualTo(expectedOrdering.Length), "ordering expressions");
            var expressions = Enumerable.Select(finder.Matches, m => m.Arguments.Last().ToString()).ToArray();
            Assert.That(expressions, Is.EqualTo(expectedOrdering), "ordering expression");
        }

        protected class TestablePackageDataService : PackageDataService
        {
            public Uri FakeRequestUri { get; set; }
            public string FakePackageProxyTargetUri { get; set; }
            public bool FakeIsRestoreOperation { get; set; }

            protected override Uri CurrentRequestUri
            {
                get
                {
                    return FakeRequestUri;
                }
            }
        }

        private class OrderingClauseFinder : ExpressionVisitor
        {
            private static readonly string[] MethodNames = new[] {"OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending"};
            private readonly List<MethodCallExpression> matches = new List<MethodCallExpression>();

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (MethodNames.Contains(node.Method.Name))
                {
                    matches.Add(node);
                }
                return base.VisitMethodCall(node);
            }

            public IEnumerable<MethodCallExpression> Matches {get { return matches; } }
        }
    }
}