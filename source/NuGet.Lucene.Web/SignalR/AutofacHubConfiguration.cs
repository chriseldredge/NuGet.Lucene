using Autofac;
using Autofac.Integration.SignalR;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NuGet.Lucene.Web.SignalR
{
    public class AutofacHubConfiguration
    {
        public static HubConfiguration CreateHubConfiguration(ILifetimeScope lifetimeScope)
        {
            var hubConfiguration = new HubConfiguration
            {
                EnableDetailedErrors = NuGetWebApiModule.ShowExceptionDetails,
                EnableJSONP = NuGetWebApiModule.EnableCrossDomainRequests,
                Resolver = new AutofacDependencyResolver(lifetimeScope)
            };

            var pipeline = hubConfiguration.Resolver.Resolve<IHubPipeline>();
            pipeline.AddModule(new SignalRLoggingModule());

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SelectiveCamelCaseContractResolver(),
                Converters = { new StringEnumConverter() },
            };

            var jsonNetSerializer = JsonSerializer.Create(settings);

            hubConfiguration.Resolver.Register(typeof(JsonSerializer), () => jsonNetSerializer);

            return hubConfiguration;
        }
    }
}