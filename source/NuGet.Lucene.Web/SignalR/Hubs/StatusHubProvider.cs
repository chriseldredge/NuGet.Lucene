using System;
using System.Reactive.Linq;
using Microsoft.AspNet.SignalR.Infrastructure;
using Ninject;
using Ninject.Activation;

namespace NuGet.Lucene.Web.SignalR.Hubs
{
    public class StatusHubProvider : IStartable
    {
        public ILucenePackageRepository Repository { get; set; }
        public IConnectionManager ConnectionManager { get; set; }

        private IDisposable statusObservable;

        public StatusHub CreateInstance(IContext context)
        {
            return new StatusHub();
        }

        public void Start()
        {
            var hub = ConnectionManager.GetHubContext<StatusHub>();

            statusObservable = Repository.StatusChanged
                .Sample(TimeSpan.FromMilliseconds(250))
                .Subscribe(status => hub.Clients.All.updateStatus(status));
        }

        public void Stop()
        {
            statusObservable.Dispose();
            statusObservable = null;
        }
    }
}