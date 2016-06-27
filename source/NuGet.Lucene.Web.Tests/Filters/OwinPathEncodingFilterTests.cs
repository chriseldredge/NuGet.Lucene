using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using NuGet.Lucene.Web.Filters;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Filters
{
    [TestFixture]
    public class OwinPathEncodingFilterTests
    {
        private OwinPathEncodingFilter filter;
        private HttpActionContext actionContext;

        class DummyController : IHttpController
        {
            public Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
        [SetUp]
        public void SetUp()
        {
            filter = new OwinPathEncodingFilter();

            actionContext = new HttpActionContext(
                new HttpControllerContext(new HttpRequestContext(), new HttpRequestMessage(), new HttpControllerDescriptor(), new DummyController()), new ReflectedHttpActionDescriptor());
        }

        [Test]
        [TestCase("http://localhost:9001/api/odata()", "http://localhost:9001/api/odata()", TestName = "Identity")]
        [TestCase("http://localhost:9001/api/odata(Id%3D10)", "http://localhost:9001/api/odata(Id=10)", TestName = "Equals")]
        [TestCase("http://localhost:9001/api/odata%2Cbar", "http://localhost:9001/api/odata,bar", TestName = "Comma")]
        [TestCase("http://localhost:9001/api/odata%28%29", "http://localhost:9001/api/odata()", TestName = "Parens")]
        [TestCase("http://localhost:9001/api/odata%28Id='Foo'%29", "http://localhost:9001/api/odata(Id='Foo')", TestName = "Parens with content")]
        public void AdjustsPath(string requestUri, string expected)
        {
            actionContext.Request.RequestUri = new Uri(requestUri, UriKind.Absolute);

            filter.OnActionExecuting(actionContext);

            Assert.That(actionContext.Request.RequestUri.AbsoluteUri, Is.EqualTo(expected));
        }
    }
}
