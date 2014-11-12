using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NuGet.Lucene.IO;

namespace NuGet.Lucene.Web.Formatters
{
    public class PackageFormDataMediaFormatter : MultipartFormDataMediaFormatter<IPackage, HashingMultipartFileStreamProvider>
    {
        private readonly ILucenePackageRepository repository;

        public PackageFormDataMediaFormatter(ILucenePackageRepository repository)
        {
            this.repository = repository;
        }

        public override HashingMultipartFileStreamProvider CreateStreamProvider()
        {
            return new HashingMultipartFileStreamProvider(repository);
        }

        public override Task<IPackage> ReadFormDataFromStreamAsync(HashingMultipartFileStreamProvider streamProvider)
        {
            IFastZipPackage package = null;

            try
            {
                var packageStream = streamProvider.ContentStreams.Single();

                package = repository.LoadStagedPackage(packageStream);
            }
            finally
            {
                if (package == null)
                {
                    foreach (var stream in streamProvider.ContentStreams)
                    {
                        repository.DiscardStagedPackage(stream);
                    }
                }
            }

            return Task.FromResult<IPackage>(package);
        }
    }

    public class HashingMultipartFileStreamProvider : MultipartStreamProvider
    {
        private readonly ILucenePackageRepository repository;
        private readonly IDictionary<string, HashingWriteStream> streams = new Dictionary<string, HashingWriteStream>();

        public HashingMultipartFileStreamProvider(ILucenePackageRepository repository)
        {
            this.repository = repository;
        }

        public IEnumerable<HashingWriteStream> ContentStreams
        {
            get { return streams.Values; }
        }

        public void AddStream(string contentDispositionHeaderValue, HashingWriteStream stream)
        {
            streams.Add(contentDispositionHeaderValue, stream);
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            var key = GetKey(headers);
            HashingWriteStream stream;
            if (!streams.TryGetValue(key, out stream))
            {
                stream = repository.CreateStreamForStagingPackage();
                AddStream(key, stream);
            }

            return stream;
        }

        static string GetKey(HttpContentHeaders headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            // N.B. Mono 3.8.0 and earlier do not correctly support
            // the headers.ContentDisposition property, so we
            // access the underlying string value instead.

            IEnumerable<string> values;
            if (!headers.TryGetValues("Content-Disposition", out values))
            {
                throw new ArgumentException("headers.ContentDisposition must not be null.");
            }

            return values.First();
        }
    }
}
