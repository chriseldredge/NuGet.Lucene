using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;

namespace NuGet.Lucene.Web.SignalR
{
    public class SignalRMapper
    {
        public NuGetWebApiRouteMapper ApiRouteMapper { get; set; }
        public INuGetWebApiSettings Settings { get; set; }

        public void MapSignalR(IAppBuilder app, HubConfiguration hubConfiguration)
        {
            app.Map("/" + ApiRouteMapper.SignalrRoutePath, map =>
            {
                if (Settings.EnableCrossDomainRequests)
                {
                    map.UseCors(CorsOptions.AllowAll);
                }
                map.RunSignalR(hubConfiguration);
            });
        }
    }
}