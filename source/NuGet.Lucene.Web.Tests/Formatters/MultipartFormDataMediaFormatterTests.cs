using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using NUnit.Framework;
using NuGet.Lucene.Tests;
using NuGet.Lucene.Web.Formatters;

namespace NuGet.Lucene.Web.Tests.Formatters
{
    [TestFixture]
    public class MultipartFormDataMediaFormatterTests
    {
        private TestableMultipartFormDataMediaFormatter formatter;
        private Stream requestBody;
        private HttpContent content;

        [SetUp]
        public void SetUp()
        {
            formatter = new TestableMultipartFormDataMediaFormatter();
            SetUpRequest("hello", incomplete: false);
        }

        [Test]
        public async Task ThrowsOnNonMultipartContent()
        {
            content.Headers.ContentType = new MediaTypeHeaderValue("application/something-weird");
            
            Func<Task> call = async () => await formatter.ReadFromStreamAsync(typeof (string), requestBody, content, null);

            await call.AssertThrowsAsync<HttpResponseException>();
        }

        [Test]
        public async Task ReadMultipartStream()
        {
            var result = await formatter.ReadFromStreamAsync(typeof(string), requestBody, content, null);

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public async Task FixesIncompleteRequests()
        {
            SetUpRequest("body", incomplete: true);

            var result = await formatter.ReadFromStreamAsync(typeof(string), requestBody, content, null);

            Assert.That(result, Is.EqualTo("body"));
        }

        private void SetUpRequest(string content, bool incomplete)
        {
            requestBody = CreateMultipartStream(content, incomplete);
            this.content = new StreamContent(requestBody);
            this.content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
            this.content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", "ExampleBoundary"));
        }

        private Stream CreateMultipartStream(string content, bool incomplete)
        {
            const string template = @"
--ExampleBoundary
Content-Disposition: form-data; name=""package""; filename=""package""
Content-Type: application/octet-stream

{0}
--ExampleBoundary--{1}";

            var message = string.Format(template, content, incomplete ? "" : Environment.NewLine);

            // Make sure message uses CRLF even when source file is in LF.
            message = Regex.Replace(message, @"(?<!\r)\n", "\r\n");

            return new MemoryStream(Encoding.ASCII.GetBytes(message));
        }

        class TestableMultipartFormDataMediaFormatter : MultipartFormDataMediaFormatter<string>
        {
            protected override Task<string> ReadFormDataFromStreamAsync(Stream stream)
            {
                return new StreamReader(stream).ReadToEndAsync();
            }
        }
    }
}