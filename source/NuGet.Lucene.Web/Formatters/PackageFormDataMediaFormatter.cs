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

        protected override HashingMultipartFileStreamProvider CreateStreamProvider()
        {
            return new HashingMultipartFileStreamProvider(repository);
        }

        protected override Task<IPackage> ReadFormDataFromStreamAsync(HashingMultipartFileStreamProvider streamProvider)
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
        private readonly IDictionary<ContentDispositionHeaderValue, HashingWriteStream> streams = new Dictionary<ContentDispositionHeaderValue, HashingWriteStream>();

        public HashingMultipartFileStreamProvider(ILucenePackageRepository repository)
        {
            this.repository = repository;
        }

        public IEnumerable<HashingWriteStream> ContentStreams
        {
            get { return streams.Values; }
        }

        public byte[] GetStreamHash(HttpContentHeaders headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            if (headers.ContentDisposition == null)
            {
                throw new ArgumentException("headers.ContentDisposition must not be null.");
            }

            return streams[headers.ContentDisposition].Hash;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            if (headers.ContentDisposition == null)
            {
                throw new ArgumentException("headers.ContentDisposition must not be null.");
            }

            HashingWriteStream stream;
            if (!streams.TryGetValue(headers.ContentDisposition, out stream))
            {
                stream = repository.CreateStreamForStagingPackage();
                streams.Add(headers.ContentDisposition, stream);
            }

            return stream;
        }
    }
}
