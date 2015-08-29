using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.Routing;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    [TestFixture]
    public class NuGetUserAgentConstraintTests : RouteTests
    {
        [Test]
        [TestCase("NuGet", true)]
        [TestCase("some_nuget_client", true)]
        [TestCase(null, true)]
        [TestCase("Mozilla", false)]
        public void MatchUserAgent(string userAgent, bool shouldMatch)
        {
            var request = SetUpRequest(userAgent);
            var constraint = new NuGetUserAgentConstraint();

            var result = constraint.Match(request, null, null, null, HttpRouteDirection.UriResolution);

            Assert.That(result, Is.EqualTo(shouldMatch));
        }

        private static HttpRequestMessage SetUpRequest(string userAgent)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/");

            if (userAgent == null) return request;

            var productInfoHeaderValue = new ProductInfoHeaderValue(new ProductHeaderValue(userAgent, "Y"));
            request.Headers.UserAgent.Add(productInfoHeaderValue);
            return request;
        }
    }
}
