using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ninject;
using Ninject.Modules;
using NuGet.Lucene.Web.SignalR.Hubs;

namespace NuGet.Lucene.Web.SignalR
{
    public class SignalRModule : NinjectModule
    {
        public override void Load()
        {
            var hubConfiguration = CreateHubConfiguration();

            Bind<HubConfiguration>().ToConstant(hubConfiguration);
            Bind<IConnectionManager>().ToMethod(_ => hubConfiguration.Resolver.Resolve<IConnectionManager>());

            Bind<StatusHubProvider>().ToSelf().InSingletonScope();
            Bind<StatusHub>().ToMethod(ctx => ctx.Kernel.Get<StatusHubProvider>().CreateInstance(ctx));
        }

        private HubConfiguration CreateHubConfiguration()
        {
            var hubConfiguration = new HubConfiguration
            {
                EnableDetailedErrors = NuGetWebApiModule.ShowExceptionDetails,
                EnableJSONP = NuGetWebApiModule.EnableCrossDomainRequests,
                Resolver = CreateDependencyResolver()
            };

            var pipeline = hubConfiguration.Resolver.Resolve<IHubPipeline>();
            pipeline.AddModule(new SignalRLoggingModule());

            return hubConfiguration;
        }

        private NinjectSignalRDependencyResolver CreateDependencyResolver()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SelectiveCamelCaseContractResolver(),
                Converters = {new StringEnumConverter()},
            };

            var jsonNetSerializer = JsonSerializer.Create(settings);

            var resolver = new NinjectSignalRDependencyResolver(Kernel);
            resolver.UseDefault<IConnectionManager>();
            resolver.Register(typeof (JsonSerializer), () => jsonNetSerializer);
            return resolver;
        }
    }
}