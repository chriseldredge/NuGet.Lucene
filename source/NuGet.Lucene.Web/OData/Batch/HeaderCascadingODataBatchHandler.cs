using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData.Batch;

namespace NuGet.Lucene.Web.OData.Batch
{
    public class HeaderCascadingODataBatchHandler : DefaultODataBatchHandler
    {
        public HeaderCascadingODataBatchHandler(HttpServer httpServer) : base(httpServer)
        {
        }

        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var items = await base.ParseBatchRequestsAsync(request, cancellationToken);

            CopyRequestHeaders(request, items);

            return items;
        }

        /// <summary>
        /// Copies HTTP Request Headers from parent request to all requests from multi-part child requests.
        /// This fixes a problem where content negotiation does not work correctly and JSON is always returned
        /// even when the client expects xml+atom.
        /// </summary>
        public virtual void CopyRequestHeaders(HttpRequestMessage request, IList<ODataBatchRequestItem> items)
        {
            var requests =
                items.OfType<OperationRequestItem>().Select(o => o.Request)
                    .Union(items.OfType<ChangeSetRequestItem>().SelectMany(cs => cs.Requests));

            foreach (var childRequest in requests)
            {
                foreach (var header in request.Headers)
                {
                    childRequest.Headers.Add(header.Key, request.Headers.GetValues(header.Key));
                }
            }
        }
    }
}
