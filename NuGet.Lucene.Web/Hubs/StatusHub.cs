using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace NuGet.Lucene.Web.Hubs
{
    [HubName("status")]
    public class StatusHub : Hub
    {
        public ILucenePackageRepository Repository { get; set; }

        public IndexingStatus GetStatus()
        {
            return Repository.GetIndexingStatus();
        }
    }
}