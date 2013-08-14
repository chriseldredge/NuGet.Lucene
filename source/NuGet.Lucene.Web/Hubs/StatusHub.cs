using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace NuGet.Lucene.Web.Hubs
{
    /// <summary>
    /// Provides SignalR powered updates as indexing activity changes.
    /// Clients can implement the <c>updateStatus</c> method on the <c>status</c>
    /// hub to receive notification when status changes.
    /// </summary>
    [HubName("status")]
    public class StatusHub : Hub
    {
        public ILucenePackageRepository Repository { get; set; }

        public RepositoryInfo GetStatus()
        {
            return Repository.GetStatus();
        }
    }
}