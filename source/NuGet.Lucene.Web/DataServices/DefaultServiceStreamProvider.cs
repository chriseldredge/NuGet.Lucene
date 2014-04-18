/*
using System;
using System.Data.Services;
using System.Data.Services.Providers;
using System.IO;

namespace NuGet.Lucene.Web.DataServices
{
    public class DefaultServiceStreamProvider : IDataServiceStreamProvider
    {
        public DefaultServiceStreamProvider()
        {
            StreamBufferSize = 65536;
            ContentType = "application/octet-stream";
        }

        public virtual Stream GetReadStream(object entity, string etag, bool? checkETagForEquality,
                                            DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public virtual Stream GetWriteStream(object entity, string etag, bool? checkETagForEquality,
                                             DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public virtual void DeleteStream(object entity, DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public virtual string GetStreamContentType(object entity, DataServiceOperationContext operationContext)
        {
            return ContentType;
        }

        public virtual Uri GetReadStreamUri(object entity, DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public virtual string GetStreamETag(object entity, DataServiceOperationContext operationContext)
        {
            return ETag;
        }

        public virtual string ResolveType(string entitySetName, DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public int StreamBufferSize { get; protected set; }
        public string ContentType { get; protected set; }
        public string ETag { get; protected set; }
    }
}
*/