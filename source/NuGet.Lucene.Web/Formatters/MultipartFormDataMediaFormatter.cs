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
            
            foreach (var header in content.Headers)
            {
                replacement.Headers.Add(header.Key, header.Value);
            }

            return replacement;
        }

        /// <summary>
        /// The NuGet client does not include a new line after the final boundary marker
        /// which causes <see cref="HttpContentMultipartExtensions.ReadAsMultipartAsync"/>
        /// to throw <see cref="IOException"/> "Unexpected end of MIME multipart stream.
        /// MIME multipart message is not complete."
        /// 
        /// This method appends a newline to the request body when missing.
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