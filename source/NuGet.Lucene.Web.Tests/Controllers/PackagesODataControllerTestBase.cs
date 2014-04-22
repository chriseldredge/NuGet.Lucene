using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;
using Moq;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Models;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    public abstract class PackagesODataControllerTestBase : ApiControllerTests<PackagesODataController>
    {
        protected Mock<IMirroringPackageRepository> repo;
        protected IEdmModel model;

        protected override PackagesODataController CreateController()
        {
            repo = new Mock<IMirroringPackageRepository>();

            var builder = new NuGetWebApiODataModelBuilder();
            builder.Build();

            model = builder.Model;

            return new PackagesODataController {Repository = repo.Object};
        }

        protected static void AssertOrderingBy(IQueryable<ODataPackage> query, params string[] expectedOrdering)
        {
            var finder = new OrderingClauseFinder();

            finder.Visit(query.Expression);

            Assert.That(Enumerable.Count(finder.Matches), Is.EqualTo(expectedOrdering.Length), "ordering expressions");
            var expressions = Enumerable.Select(finder.Matches, m => m.Arguments.Last().ToString()).ToArray();
            Assert.That(expressions, Is.EqualTo(expectedOrdering), "ordering expression");
        }

        private class OrderingClauseFinder : ExpressionVisitor
        {
            private static readonly string[] MethodNames = new[] { "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending" };
            private readonly List<MethodCallExpression> matches = new List<MethodCallExpression>();

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (MethodNames.Contains(node.Method.Name))
                {
                    matches.Add(node);
                }
                return base.VisitMethodCall(node);
            }

            public IEnumerable<MethodCallExpression> Matches { get { return matches; } }
        }

        protected ODataQueryOptions<ODataPackage> SetUpRequestWithOptions(string path)
        {
            SetUpRequest(RouteNames.Packages.Feed, HttpMethod.Post, path);
            return new ODataQueryOptions<ODataPackage>(new ODataQueryContext(model, typeof(ODataPackage)), request);
        }
    }
}