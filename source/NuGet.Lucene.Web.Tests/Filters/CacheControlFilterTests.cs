using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using NuGet.Lucene.Web.Filters;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Filters
{
    [TestFixture]
    public class CacheControlFilterTests
    {
        private CacheControlFilter filter;
        private HttpActionExecutedContext context;

        [SetUp]
        public void SetUp()
        {
            filter = new CacheControlFilter();
            context = new HttpActionExecutedContext
            {
                ActionContext = new HttpActionContext(),
                Response = new HttpResponseMessage()
            };
        }

        [Test]
        public void SetsHeader()
        {
            filter.OnActionExecutedAsync(context, CancellationToken.None);

            Assert.That(context.Response.Headers.CacheControl, Is.Not.Null);
        }

        [Test]
        public void DoesNotReplaceHeader()
        {
            var originalHeader = new CacheControlHeaderValue();
            context.Response.Headers.CacheControl = originalHeader;

            filter.OnActionExecutedAsync(context, CancellationToken.None);

            Assert.That(context.Response.Headers.CacheControl, Is.SameAs(originalHeader));
        }

        [Test]
        public void DoesNothingWhenNoResponse()
        {
            context.Response = null;

            filter.OnActionExecutedAsync(context, CancellationToken.None);

            Assert.That(context.Response, Is.Null);
        }
    }
}
