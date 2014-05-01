using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Ninject;

namespace NuGet.Lucene.Web.SignalR
{
    public class NinjectSignalRDependencyResolver : DefaultDependencyResolver
    {
        private readonly IKernel kernel;
        private readonly ISet<Type> blacklist = new HashSet<Type>();

        public NinjectSignalRDependencyResolver(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public override object GetService(Type serviceType)
        {
            object svc;

            if (!blacklist.Contains(serviceType) && kernel.GetBindings(serviceType).Any())
            {
                svc = kernel.TryGet(serviceType);

                if (svc != null)
                {
                    return svc;
                }
            }

            svc = base.GetService(serviceType);

            if (svc != null )
            {
                kernel.Inject(svc);
            }

            return svc;
        }

        public void UseDefault<T>()
        {
            blacklist.Add(typeof(T));
        }
    }
}