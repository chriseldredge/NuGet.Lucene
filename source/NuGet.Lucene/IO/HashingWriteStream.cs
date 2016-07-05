using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Lucene.IO
{
    public class HashingWriteStream : Stream
    {
        private readonly string fileLocation;
        private readonly Stream stream;
        private readonly HashAlgorithm hashAlgorithm;
        private bool disposed;
        private byte[] hash;

        public HashingWriteStream(string fileLocation, Stream stream, HashAlgorithm hashAlgorithm)
        {
            this.fileLocation = fileLocation;
            this.stream = stream;
            this.hashAlgorithm = hashAlgorithm;
        }

        public byte[] Hash
        {
            get
            {
                if (!disposed)
                {
                    throw new InvalidOperationException("Stream must be closed before Hash can be computed.");
                }
                return hash;
            }
        }

        public string FileLocation
        {
            get { return fileLocation; }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
            hashAlgorithm.TransformBlock(buffer, offset, count, null, 0);
        }

        public override void Flush()
        {
            stream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing || disposed) return;

            stream.Dispose();
            hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
            hash = hashAlgorithm.Hash;
            hashAlgorithm.Dispose();
            disposed = true;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(buffer, offset, count, cancellationToken);
            hashAlgorithm.TransformBlock(buffer, offset, count, null, 0);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return stream.FlushAsync(cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException("Use WriteAsync.");
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return stream.Length; }
        }

        public override long Position
        {
            get { return stream.Position; }
            set { throw new NotSupportedException(); }
        }
    }
}
