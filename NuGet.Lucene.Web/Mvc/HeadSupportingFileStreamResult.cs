using System;
using System.IO;
using System.Web.Mvc;

namespace NuGet.Lucene.Web.Mvc
{
    public class HeadSupportingFileStreamResult : FileStreamResult
    {
        private const string Rfc2822DateFormat = "ddd, dd MMM yyyy hh:mm:ss 'GMT'";

        public HeadSupportingFileStreamResult(Stream fileStream, string contentType) : base(fileStream, contentType)
        {
        }

        public long? ContentLength { get; set; }
        public DateTimeOffset? LastModified { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.RequestContext.HttpContext.Response;

            if (ContentLength.HasValue)
            {
                response.AddHeader("Content-Length", ContentLength.Value.ToString());
                response.BufferOutput = false;
            }
            
            if (LastModified.HasValue)
            {
                response.AddHeader("Last-Modified", LastModified.Value.ToUniversalTime().ToString(Rfc2822DateFormat));
            }

            if (string.Equals(context.HttpContext.Request.HttpMethod, "HEAD", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            base.ExecuteResult(context);
        }
    }
}