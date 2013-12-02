using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Ninject;
using Ninject.Modules;
using NuGet.Lucene.Web.Hubs;
using Owin;

namespace NuGet.Lucene.Web
{
    public class SignalRStartup
    {
        private static IKernel kernel;

        public void Configuration(IAppBuilder app)
        {
            if (kernel == null)
            {
                throw new InvalidOperationException("Ninject kernel must be set first.");
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SelectiveCamelCaseContractResolver(),
                Converters = { new StringEnumConverter() },
            };

            var jsonNetSerializer = JsonSerializer.Create(settings);

            var resolver = new NinjectSignalRDependencyResolver(kernel);
            resolver.Register(typeof(JsonSerializer), () => jsonNetSerializer);

            var hubConfiguration = new HubConfiguration
            {
                EnableDetailedErrors = NuGetWebApiModule.ShowExceptionDetails,
                EnableJSONP = NuGetWebApiModule.EnableCrossDomainRequests,
                Resolver = resolver
            };

            app.MapSignalR("/api/signalr", hubConfiguration);

            var connectionManager = resolver.Resolve<IConnectionManager>();

            var hub = connectionManager.GetHubContext<StatusHub>();

            var repository = kernel.Get<ILucenePackageRepository>();

            repository.StatusChanged
                .Sample(TimeSpan.FromMilliseconds(250))
                .Subscribe(status => hub.Clients.All.updateStatus(status));
        }

        internal static void SetKernel(IKernel kernel)
        {
            SignalRStartup.kernel = kernel;
        }
    }

    public class SignalRModule : NinjectModule
    {
        public override void Load()
        {
            SignalRStartup.SetKernel(Kernel);
            Kernel.Bind<StatusHub>().ToSelf();
        }
    }

    public class NinjectSignalRDependencyResolver : DefaultDependencyResolver
    {
        private readonly IKernel kernel;

        public NinjectSignalRDependencyResolver(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public override object GetService(Type serviceType)
        {
            object svc;

            if (kernel.GetBindings(serviceType).Any())
            {
                svc = kernel.TryGet(serviceType);

                if (svc != null)
                {
                    return svc;
                }
            }

            svc = base.GetService(serviceType);

            if (svc != null)
            {
                kernel.Inject(svc);
            }

            return svc;
        }
    }

    /// <summary>
    /// Uses default contract resolver for types in the SignalR assembly
    /// and camel case for all other types.
    /// </summary>
    public class SelectiveCamelCaseContractResolver : IContractResolver
    {
        private readonly Assembly signalrAssembly;
        private readonly IContractResolver camelCaseContractResolver;
        private readonly IContractResolver defaultContractSerializer;

        public SelectiveCamelCaseContractResolver()
        {
            defaultContractSerializer = new DefaultContractResolver();
            camelCaseContractResolver = new CamelCasePropertyNamesContractResolver();
            signalrAssembly = typeof(Connection).Assembly;
        }

        public JsonContract ResolveContract(Type type)
        {
            if (type.Assembly.Equals(signalrAssembly))
                return defaultContractSerializer.ResolveContract(type);

            return camelCaseContractResolver.ResolveContract(type);
        }
    }
}