using System.Web.Http.Dependencies;
using Ninject;

namespace NuGet.Lucene.Web.Ninject
{
    public class NinjectResolver : NinjectScope, System.Web.Http.Dependencies.IDependencyResolver
    {
        private readonly IKernel kernel;

        public NinjectResolver(IKernel kernel)
            : base(kernel)
        {
            this.kernel = kernel;
        }

        public IDependencyScope BeginScope()
        {
            return new NinjectScope(kernel.BeginBlock());
        }
    }
}