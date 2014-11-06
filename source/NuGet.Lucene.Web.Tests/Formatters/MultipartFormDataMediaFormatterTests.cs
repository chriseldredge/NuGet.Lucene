using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using NuGet.Lucene.Tests;
using NuGet.Lucene.Web.Formatters;
using NUnit.Framework;

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
            SetUpRequest("hello", useWrongNewlineOnFinalBoundary:false, omitFinalNewline: false);
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
            SetUpRequest("body", useWrongNewlineOnFinalBoundary:false, omitFinalNewline:true);

            var result = await formatter.ReadFromStreamAsync(typeof(string), requestBody, content, null);

            Assert.That(result, Is.EqualTo("body"));
        }

        [Test]
        public async Task FixesWrongNewlineOnEndBoundary()
        {
            SetUpRequest("body", useWrongNewlineOnFinalBoundary:true, omitFinalNewline:false);

            var result = await formatter.ReadFromStreamAsync(typeof(string), requestBody, content, null);

            Assert.That(result, Is.EqualTo("body"));
        }

        [Test]
        public async Task FixesWrongNewlineOnEndBoundaryAndMissingFinalNewline()
        {
            SetUpRequest("body", useWrongNewlineOnFinalBoundary:true, omitFinalNewline:true);

            var result = await formatter.ReadFromStreamAsync(typeof(string), requestBody, content, null);

            Assert.That(result, Is.EqualTo("body"));
        }

        private void SetUpRequest(string content, bool useWrongNewlineOnFinalBoundary, bool omitFinalNewline)
        {
            requestBody = CreateMultipartStream(content, useWrongNewlineOnFinalBoundary, omitFinalNewline);
            this.content = new StreamContent(requestBody);
            this.content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
            this.content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", "ExampleBoundary"));
        }

        private Stream CreateMultipartStream(string content, bool useWrongNewlineOnFinalBoundary, bool omitFinalNewline)
        {
            const string template = @"
--ExampleBoundary
Content-Disposition: form-data; name=""package""; filename=""package""
Content-Type: application/octet-stream

{0}{1}--ExampleBoundary--{2}";

            // Make sure message uses CRLF even when source file is in LF.
            var message = Regex.Replace(template, @"(?<!\r)\n", "\r\n");

            message = string.Format(message, content,
                useWrongNewlineOnFinalBoundary ? "\n" : "\r\n",
                omitFinalNewline ? "" : "\r\n");

            return new MemoryStream(Encoding.ASCII.GetBytes(message));
        }

        class TestableMultipartFormDataMediaFormatter : MultipartFormDataMediaFormatter<string, MultipartMemoryStreamProvider>
        {
            protected override MultipartMemoryStreamProvider CreateStreamProvider()
            {
                return new MultipartMemoryStreamProvider();
            }

            protected override async Task<string> ReadFormDataFromStreamAsync(MultipartMemoryStreamProvider streamProvider)
            {
                return await streamProvider.Contents.Single().ReadAsStringAsync();
            }
        }
    }
}
