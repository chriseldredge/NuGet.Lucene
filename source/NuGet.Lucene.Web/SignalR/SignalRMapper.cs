using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;

namespace NuGet.Lucene.Web.SignalR
{
    public class SignalRMapper
    {
        public HubConfiguration HubConfiguration { get; set; }
        public NuGetWebApiRouteMapper ApiRouteMapper { get; set; }

        public void MapSignalR(IAppBuilder app)
        {
            app.Map("/" + ApiRouteMapper.SignalrRoutePath, map =>
            {
                if (NuGetWebApiModule.EnableCrossDomainRequests)
                {
                    map.UseCors(CorsOptions.AllowAll);
                }
                map.RunSignalR(HubConfiguration);
            });
        }
    }
}