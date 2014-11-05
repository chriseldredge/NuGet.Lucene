using System.IO;
using System.Text;
using NuGet.Lucene.Web.Formatters;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Formatters
{
    [TestFixture]
    public class MalformedMultipartFixingStreamTests
    {
        [Test]
        public void Simple()
        {
            const string body = "the message\r\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var result = stream.ReadToEnd();

            Assert.That(result, Is.EqualTo(body));
        }

        [Test]
        public void HandlesMultipleReplacements()
        {
            const string body = "the message\n--foo\r\nanother message\n--foo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "--foo");
            var result = stream.ReadToEnd();

            Assert.That(result, Is.EqualTo("the message\r\n--foo\r\nanother message\r\n--foo\r\n"));
        }

        [Test]
        public void FixNewlineInSingleRead()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var result = stream.ReadToEnd();

            Assert.That(result, Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public void PreserveCharInBoundRead()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = stream.Read(buffer, 0, body.Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + stream.ReadToEnd(), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public void CachesPartialMatch()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = stream.Read(buffer, 0, "the message\nf".Length);
            
            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + stream.ReadToEnd(), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public void CachesPartialMatch_NoMatchOnReadAhead_EndOfStream()
        {
            const string body = "the message\nfo";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foobar");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = stream.Read(buffer, 0, "the message\nf".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + stream.ReadToEnd(), Is.EqualTo("the message\nfo"));
        }

        [Test]
        public void CachesPartialMatch_NoMatchOnReadAhead()
        {
            const string body = "the message\nfor the record";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = stream.Read(buffer, 0, "the message\nf".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + stream.ReadToEnd(), Is.EqualTo("the message\nfor the record"));
        }

        [Test]
        public void CachesPreviousCharOnPartialMatch()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = stream.Read(buffer, 0, "the message".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + stream.ReadToEnd(), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public void CachesPreviousCharOnPartialMatchBufferOffset()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[2048];
            var bytesRead = stream.Read(buffer, 1024, 1024);

            Assert.That(Encoding.UTF8.GetString(buffer, 1024, bytesRead), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public void CachesPreviousCharOnPartialMatchBufferOffsetMultiRead()
        {
            const string body = "the message\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[2048];
            var bytesRead = stream.Read(buffer, 1024, "the message\nf".Length);
            bytesRead += stream.Read(buffer, 1024 + bytesRead, 1024-bytesRead);

            Assert.That(Encoding.UTF8.GetString(buffer, 1024, bytesRead), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public void CachesPreviousCharOnPartialMatchWithCorrectNewline()
        {
            const string body = "the message\r\nfoo\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[realStream.Length + 1];
            var bytesRead = stream.Read(buffer, 0, "the message\r".Length);

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + stream.ReadToEnd(), Is.EqualTo("the message\r\nfoo\r\n"));
        }

        [Test]
        public void FixNewlineAtStartShortRead()
        {
            const string body = "\nfoo\r\nthe message\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            var stream = new MalformedMultipartFixingStream(realStream, "foo");
            var buffer = new byte[2];
            var bytesRead = stream.Read(buffer, 0, 2);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead) + stream.ReadToEnd(), Is.EqualTo("\r\nfoo\r\nthe message\r\n"));
        }

        [Test]
        public void FixNewlineAtStartSingleRead()
        {
            const string body = "\nfoo\r\nthe message\r\n";
            var realStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            
            var stream = new MalformedMultipartFixingStream(realStream, "foo");

            Assert.That(stream.ReadToEnd(), Is.EqualTo("\r\nfoo\r\nthe message\r\n"));
        }
    }
}
