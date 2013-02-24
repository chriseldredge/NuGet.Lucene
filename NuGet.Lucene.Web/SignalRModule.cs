using System;
using System.Reflection;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Ninject;
using Ninject.Modules;
using NuGet.Lucene.Web.Hubs;

namespace NuGet.Lucene.Web
{
    public class SignalRModule : NinjectModule
    {
        public override void Load()
        {
            var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCaseContractResolver(),
                    Converters = { new StringEnumConverter() }
                };

            var jsonNetSerializer = new JsonNetSerializer(settings);
            
            GlobalHost.DependencyResolver = new NinjectSignalRDependencyResolver(Kernel);
            GlobalHost.DependencyResolver.Register(typeof(IJsonSerializer), () => jsonNetSerializer);

            var hub = GlobalHost.ConnectionManager.GetHubContext<StatusHub>();

            Kernel.Get<ILucenePackageRepository>().StatusChanged
                //.Throttle(TimeSpan.FromMilliseconds(500))
                  .Subscribe(status => hub.Clients.All.updateStatus(status));
        }

        public class CamelCaseContractResolver : IContractResolver
        {
            private readonly Assembly signalrAssembly;
            private readonly IContractResolver camelCaseContractResolver;
            private readonly IContractResolver defaultContractSerializer;

            public CamelCaseContractResolver()
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

        public class NinjectSignalRDependencyResolver : DefaultDependencyResolver
        {
            private readonly IKernel kernel;

            public NinjectSignalRDependencyResolver(IKernel kernel)
            {
                this.kernel = kernel;
            }

            public override object GetService(Type serviceType)
            {
                var svc = kernel.TryGet(serviceType);

                if (svc != null) return svc;

                svc = base.GetService(serviceType);

                if (svc != null)
                {
                    kernel.Inject(svc);
                }

                return svc;
            }
        }
    }
}