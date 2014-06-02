using System;
using System.Reactive.Linq;
using Microsoft.AspNet.SignalR.Infrastructure;
using NuGet.Lucene.Web.SignalR.Hubs;

namespace NuGet.Lucene.Web.SignalR
{
    public class StatusHubUpdateBroadcaster : IDisposable
    {
        public ILucenePackageRepository Repository { get; set; }
        public IConnectionManager ConnectionManager { get; set; }

        private IDisposable statusObservable;

        public void Start()
        {
            var hub = ConnectionManager.GetHubContext<StatusHub>();

            statusObservable = Repository.StatusChanged
                .Sample(TimeSpan.FromMilliseconds(250))
                .Subscribe(status => hub.Clients.All.updateStatus(status));
        }

        public void Dispose()
        {
            if (statusObservable != null)
            {
                statusObservable.Dispose();
                statusObservable = null;
            }
        }
    }
}