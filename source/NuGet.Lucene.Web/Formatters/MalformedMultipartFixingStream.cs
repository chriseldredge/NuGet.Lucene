using System;
using System.IO;
using System.Text;

namespace NuGet.Lucene.Web.Formatters
{
    /// <summary>
    /// NuGet client (as of 2.8.2) uses wrong newline character before closing
    /// multi-part boundary when running on mono and <see cref="Environment.NewLine"/>
    /// is <c>\n</c>. It should always use <c>\r\n</c> but does not.
    /// 
    /// This stream decorator fixes these boundaries to be correct.
    /// </summary>
    public class MalformedMultipartFixingStream : Stream
    {
        private readonly Stream stream;
        private readonly MemoryStream peekBuffer = new MemoryStream();
        private readonly byte[] boundary;
        private byte lastByteRead;

        public MalformedMultipartFixingStream(Stream stream, string boundary)
        {
            this.stream = stream;
            this.boundary = Encoding.UTF8.GetBytes('\n' + boundary);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = BufferedRead(buffer, offset, count);
            if (bytesRead > 0)
            {
                lastByteRead = buffer[offset + bytesRead - 1];
            }

            return bytesRead;
        }

        public int BufferedRead(byte[] buffer, int offset, int count)
        {
            var bytesRead = peekBuffer.Read(buffer, offset, count);

            var totalBytesRead = bytesRead;

            if (bytesRead < count)
            {
                totalBytesRead += stream.Read(buffer, offset + bytesRead, count - bytesRead);
            }

            if (totalBytesRead == bytesRead)
            {
                return totalBytesRead;
            }

            peekBuffer.Seek(0, SeekOrigin.Begin);
            peekBuffer.SetLength(0);

            for (var matchPosition = 0; matchPosition < totalBytesRead; matchPosition++)
            {
                var matchLength = 0;
                var isMatch = true;
                while (matchLength < boundary.Length && matchPosition + matchLength < totalBytesRead && isMatch)
                {
                    isMatch = buffer[offset + matchPosition + matchLength] == boundary[matchLength];
                    matchLength++;
                }

                if (!isMatch) continue;

                totalBytesRead = ReplaceMalformedBoundary(buffer, offset, count, matchPosition, matchLength, totalBytesRead);
            }

            peekBuffer.Seek(0, SeekOrigin.Begin);

            return totalBytesRead;
        }

        private int ReplaceMalformedBoundary(byte[] buffer, int offset, int count, int matchIndex, int matchLength, int totalBytesRead)
        {
            var origOffset = offset;
            var origBuffer = buffer;
            var totalAdditionalBytesRead = 0;

            if (matchLength < boundary.Length)
            {
                var tmp = new byte[totalBytesRead + boundary.Length - matchLength];
                Array.Copy(buffer, offset, tmp, 0, totalBytesRead);
                int additionalBytesRead;
                var bytesToRead = boundary.Length - matchLength;
                do
                {
                    additionalBytesRead = stream.Read(tmp, totalBytesRead + totalAdditionalBytesRead, bytesToRead);
                    totalAdditionalBytesRead += additionalBytesRead;
                    bytesToRead -= additionalBytesRead;
                } while (additionalBytesRead != 0 && bytesToRead > 0);

                buffer = tmp;
                offset = 0;
            }

            var prev = matchIndex == 0 ? lastByteRead : buffer[offset + matchIndex - 1];
            if (prev == '\r')
            {
                if (!ReferenceEquals(buffer, origBuffer) && totalAdditionalBytesRead > 0)
                {
                    peekBuffer.Write(buffer, totalBytesRead, totalAdditionalBytesRead);
                }
                return totalBytesRead;
            }

            var last = buffer[offset + totalBytesRead - 1];
            for (var k = totalBytesRead - 1; k > matchIndex; k--)
            {
                buffer[offset + k] = buffer[offset + k - 1];
            }
            buffer[offset + matchIndex] = (byte) '\r';

            if (totalBytesRead < count)
            {
                buffer[offset + totalBytesRead] = last;
                totalBytesRead++;
            }
            else
            {
                peekBuffer.WriteByte(last);
            }
            
            if (!ReferenceEquals(origBuffer, buffer))
            {
                Array.Copy(buffer, 0, origBuffer, origOffset, totalBytesRead);
                if (buffer.Length > totalBytesRead)
                {
                    peekBuffer.Write(buffer, totalBytesRead, totalAdditionalBytesRead);
                }
            }

            return totalBytesRead;
        }

        public override bool CanRead
        {
            get { return stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
        }

        #region NotSupported
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
