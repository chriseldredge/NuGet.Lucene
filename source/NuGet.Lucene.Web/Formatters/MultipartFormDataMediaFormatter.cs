using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace NuGet.Lucene.Web.Formatters
{
    public abstract class MultipartFormDataMediaFormatter<TModel, TMultipartStreamProvider> : MediaTypeFormatter where TMultipartStreamProvider : MultipartStreamProvider
    {
        protected MultipartFormDataMediaFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("multipart/form-data"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
        }

        protected abstract TMultipartStreamProvider CreateStreamProvider();
        protected abstract Task<TModel> ReadFormDataFromStreamAsync(TMultipartStreamProvider streamProvider);

        public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            content.Headers.ContentType = ParseContentType(content);

            if (!content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var multipartStream = await FixIncompleteMultipartContent(content);

            content = ReplaceContent(content, multipartStream);

            var streamProvider = CreateStreamProvider();
            await content.ReadAsMultipartAsync(streamProvider);
            await streamProvider.ExecutePostProcessingAsync();
            return await ReadFormDataFromStreamAsync(streamProvider);
        }

        private static HttpContent ReplaceContent(HttpContent content, Stream multipartStream)
        {
            var replacement = new StreamContent(multipartStream);
            replacement.Headers.ContentType = content.Headers.ContentType;
            return replacement;
        }

        /// <summary>
        /// Manually parse malformed content header that does not quote boundary parameter value.
        /// Fixes bug on mono where <see cref="HttpContentHeaders.ContentType"/> is null when
        /// boundary parameter is not quoted.
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
        ///  * Ensure eol style is CRLF at ending boundary marker
        /// </summary>
        private async Task<Stream> FixIncompleteMultipartContent(HttpContent content)
        {
            var boundaryParam = content.Headers.ContentType.Parameters.Single(p => p.Name == "boundary");
            var boundary = "--" + boundaryParam.Value.Trim('\'', '"');
            return new MalformedMultipartFixingStream(await content.ReadAsStreamAsync(), boundary);
        }

        public override bool CanReadType(Type type)
        {
            return typeof(TModel).IsAssignableFrom(type);
        }

        public override bool CanWriteType(Type type)
        {
            return false;
        }
    }
}
