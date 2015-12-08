using System.IO;
using System.Text;
using System.Threading.Tasks;
using NuGet.Lucene.IO;
using NuGet.Lucene.Web.Formatters;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Formatters
{
    [TestFixture]
    public class MalformedMultipartFixingStreamTests
    {
        [Test]
        public async Task Simple()
        {
            const string body = "the message\r\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var result = (await ReadToEndAsync(stream));

            Assert.That(result, Is.EqualTo(body));
        }

        [Test]
        public async Task HandlesMultipleReplacements()
        {
            const string body = "the message\n--foo\r\nanother message\n--foo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "--foo");
            var result = (await ReadToEndAsync(stream));

            Assert.That(result, Is.EqualTo("the message\r\n--foo\r\nanother message\r\n--foo\r\n"));
        }

        [Test]
        public async Task FixNewlineInSingleRead()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var result = (await ReadToEndAsync(stream));

            Assert.That(result, Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public async Task PreserveCharInBoundRead()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = await stream.ReadAsync(buffer, 0, body.Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + (await ReadToEndAsync(stream)), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public async Task CachesPartialMatch()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = await stream.ReadAsync(buffer, 0, "the message\nf".Length);
            bytesRead += await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + (await ReadToEndAsync(stream)), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public async Task CachesPartialMatch_NoMatchOnReadAhead_EndOfStream()
        {
            const string body = "the message\nfo";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foobar");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = await stream.ReadAsync(buffer, 0, "the message\nf".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + (await ReadToEndAsync(stream)), Is.EqualTo("the message\nfo"));
        }

        [Test]
        public async Task CachesPartialMatch_NoMatchOnReadAhead()
        {
            const string body = "the message\nfor the record";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = await stream.ReadAsync(buffer, 0, "the message\nf".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + (await ReadToEndAsync(stream)), Is.EqualTo("the message\nfor the record"));
        }

        [Test]
        public async Task CachesPreviousCharOnPartialMatch()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = await stream.ReadAsync(buffer, 0, "the message".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + (await ReadToEndAsync(stream)), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public async Task CachesPreviousCharOnPartialMatchBufferOffset()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[2048];
            var bytesRead = await stream.ReadAsync(buffer, 1024, 1024);

            Assert.That(Encoding.UTF8.GetString(buffer, 1024, bytesRead), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public async Task CachesPreviousCharOnPartialMatchBufferOffsetMultiRead()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[2048];
            var bytesRead = await stream.ReadAsync(buffer, 1024, "the message\nf".Length);
            bytesRead += await stream.ReadAsync(buffer, 1024 + bytesRead, 1024-bytesRead);

            Assert.That(Encoding.UTF8.GetString(buffer, 1024, bytesRead), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public async Task CachesPreviousCharOnPartialMatchWithCorrectNewline()
        {
            const string body = "the message\r\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = await stream.ReadAsync(buffer, 0, "the message\r".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + (await ReadToEndAsync(stream)), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public async Task FixNewlineAtStartShortRead()
        {
            const string body = "\nfoo\r\nthe message\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[2];
            var bytesRead = await stream.ReadAsync(buffer, 0, 2);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + (await ReadToEndAsync(stream)), Is.EqualTo("\r\nfoo\r\nthe message\r\n"));
        }

        private async Task<string> ReadToEndAsync(Stream stream)
        {
            return await new StreamReader(stream).ReadToEndAsync();
        }

        [Test]
        public async Task FixNewlineAtStartSingleRead()
        {
            const string body = "\nfoo\r\nthe message\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");

            Assert.That((await ReadToEndAsync(stream)), Is.EqualTo("\r\nfoo\r\nthe message\r\n"));
        }
    }
}
