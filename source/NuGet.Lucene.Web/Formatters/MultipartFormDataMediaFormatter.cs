using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace NuGet.Lucene.Web.Formatters
{
    public abstract class MultipartFormDataMediaFormatter<T> : MediaTypeFormatter
    {
        protected MultipartFormDataMediaFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("multipart/form-data"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
        }

        protected abstract Task<T> ReadFormDataFromStreamAsync(Stream stream);

        public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            content.Headers.ContentType = ParseContentType(content);

            if (!content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var multipartStream = await FixIncompleteMultipartContent(content);

            content = ReplaceContent(content, multipartStream);

            var parts = await content.ReadAsMultipartAsync();

            var fileContent = parts.Contents.FirstOrDefault(p => SupportedMediaTypes.Contains(p.Headers.ContentType));

            if (fileContent == null)
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            using (var stream = await fileContent.ReadAsStreamAsync())
            {
                return await ReadFormDataFromStreamAsync(stream);
            }
        }

        private static HttpContent ReplaceContent(HttpContent content, Stream multipartStream)
        {
            var replacement = new StreamContent(multipartStream);
            replacement.Headers.ContentLength = multipartStream.Length;
            replacement.Headers.ContentType = content.Headers.ContentType;
            return replacement;
        }

        /// <summary>
        /// Manually parse malformed content header that does not quote boundary parameter value.
        /// </summary>
        static MediaTypeHeaderValue ParseContentType(HttpContent content)
        {
            var contentTypes = content.Headers.Where(h => h.Key == "Content-Type").ToArray();

            if (contentTypes.Length == 0)
            {
                return null;
            }

            var contentType = contentTypes.Last();
            var typeAndParameters = contentType.Value.First()
                                        .Split(new[] {';'}, 2);

            var header = new MediaTypeHeaderValue(typeAndParameters[0]);

            if (typeAndParameters.Length == 1)
            {
                return header;
            }

            foreach (var param in typeAndParameters[1].Split(';'))
            {
                var kv = param.Split(new[] {'='}, 2);
                var key = kv[0].Trim();
                var value = kv[1].Trim();
                if (value[0] != '"' && value[0] != '\'')
                {
                    value = '"' + value + '"';
                }
                header.Parameters.Add(new NameValueHeaderValue(key, value));
            }

            return header;
        }

        /// <summary>
        /// Fixes the following non-compliant issues with nuget client (as of 2.8.2):
        ///  * Ensure new line after at end of stream
        ///  * Ensure eol style is CRLF at ending boundary marker
        /// </summary>
        private async Task<Stream> FixIncompleteMultipartContent(HttpContent content)
        {
            var capacity = (int) content.Headers.ContentLength.GetValueOrDefault(128000) + 2;
            var buffer = new MemoryStream(capacity);

            await content.CopyToAsync(buffer);

            var bytes = buffer.GetBuffer();

            if (bytes[buffer.Length - 2] != '\r' || bytes[buffer.Length - 1] != '\n')
            {
                buffer.Write(Encoding.ASCII.GetBytes("\r\n"), 0, 2);
            }

            var boundary = content.Headers.ContentType.Parameters.Single(p => p.Name == "boundary").Value;

            var position = buffer.Length - boundary.Length - 6;
            if (bytes[position] != '\r' && bytes[position+1] == '\n')
            {
                var oldCapacity = buffer.Capacity;
                buffer.SetLength(buffer.Length + 1);
                if (oldCapacity != buffer.Capacity)
                {
                    bytes = buffer.GetBuffer();
                }

                for (var i = buffer.Length - 1; i > position; i--)
                {
                    bytes[i] = bytes[i - 1];
                }

                bytes[position + 1] = (byte)'\r';
            }
            buffer.Seek(0, SeekOrigin.Begin);

            return buffer;
        }

        public override bool CanReadType(Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }

        public override bool CanWriteType(Type type)
        {
            return false;
        }
    }
}