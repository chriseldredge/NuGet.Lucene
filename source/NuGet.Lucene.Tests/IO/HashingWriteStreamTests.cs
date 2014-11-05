using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NuGet.Lucene.IO;
using NUnit.Framework;

namespace NuGet.Lucene.Tests.IO
{
    [TestFixture]
    class HashingWriteStreamTests
    {
        private static readonly string text = new string('c', 8192);
        private static readonly byte[] textBytes = Encoding.UTF8.GetBytes(text);

        [Test]
        public void Write()
        {
            var source = new MemoryStream();
            var stream = new HashingWriteStream(null, source, new SHA256CryptoServiceProvider());

            stream.Write(textBytes, 0, textBytes.Length);
            
            Assert.That(Encoding.UTF8.GetString(source.ToArray()), Is.EqualTo(text));
        }

        [Test]
        public void ComputesHash()
        {
            var stream = new HashingWriteStream(null, new MemoryStream(), new SHA256CryptoServiceProvider());

            stream.Write(textBytes, 0, textBytes.Length);
            stream.Close();

            var expectedHash = new SHA256CryptoServiceProvider().ComputeHash(textBytes);

            Assert.That(stream.Hash, Is.EqualTo(expectedHash));
        }

        [Test]
        public async Task CopyToAsync_Writes()
        {
            var source = new MemoryStream(textBytes);
            var dest = new MemoryStream();
            var stream = new HashingWriteStream(null, dest, new SHA256CryptoServiceProvider());

            await source.CopyToAsync(stream);

            Assert.That(dest.ToArray(), Is.EqualTo(textBytes));
        }

        [Test]
        public async Task CopyToAsync_ComputesHash()
        {
            var source = new MemoryStream(textBytes);
            var dest = new MemoryStream();
            var stream = new HashingWriteStream(null, dest, new SHA256CryptoServiceProvider());

            await source.CopyToAsync(stream);
            stream.Close();

            var expectedHash = new SHA256CryptoServiceProvider().ComputeHash(textBytes);
            Assert.That(stream.Hash, Is.EqualTo(expectedHash));
        }
    }
}
