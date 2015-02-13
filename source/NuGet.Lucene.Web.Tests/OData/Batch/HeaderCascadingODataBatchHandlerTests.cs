using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData.Batch;
using NuGet.Lucene.Web.OData.Batch;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.OData.Batch
{
    [TestFixture]
    class HeaderCascadingODataBatchHandlerTests
    {
        private HeaderCascadingODataBatchHandler handler;

        [SetUp]
        public void SetUp()
        {
            handler = new HeaderCascadingODataBatchHandler(new HttpServer(new HttpConfiguration()));
        }

        [Test]
        public async Task CopiesHeadersToChild()
        {
            var request = SetUpRequest("GET http://example.com/api/odata/Packages HTTP/1.1\r\n");

            var result = await ParseBatchRequestAsync(request);

            Assert.That(result.Select(r => r.Headers.GetValues("Accept").SingleOrDefault()).ToArray(), Is.EqualTo(new[] {"application/xml+atom"}));
        }

        [Test]
        public async Task CopiesHeadersToAllChildren()
        {
            var request = SetUpRequest("GET http://example.com/api/odata/Packages HTTP/1.1\r\n", "GET http://example.com/api/odata/GetUpdates HTTP/1.1\r\n");

            var result = await ParseBatchRequestAsync(request);

            Assert.That(result.Select(r => r.Headers.GetValues("Accept").SingleOrDefault()).ToArray(), Is.EqualTo(new[] { "application/xml+atom", "application/xml+atom" }));
        }

        private HttpRequestMessage SetUpRequest(params string[] parts)
        {
            var content = new MultipartContent("mixed", "xyz-boundary");

            foreach (var part in parts)
            {
                var nestedContent = new StringContent(part);
                nestedContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
                nestedContent.Headers.Add("Content-Transfer-Encoding", "binary");

                content.Add(nestedContent);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "http://example.com/api/odata/$batch")
            {
                Content = content
            };

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml+atom"));

            return request;
        }

        private async Task<IList<HttpRequestMessage>> ParseBatchRequestAsync(HttpRequestMessage request)
        {
            var result = await handler.ParseBatchRequestsAsync(request, CancellationToken.None);

            return result.OfType<OperationRequestItem>().Select(o => o.Request).ToList();
        }
    }
}
