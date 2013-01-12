using System.Web.Http;
using Ninject;
using Ninject.Web.Common;
using NuGet.Lucene.Web.Ninject;

[assembly: WebActivator.PreApplicationStartMethod(typeof(NuGet.Lucene.Web.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivator.ApplicationShutdownMethodAttribute(typeof(NuGet.Lucene.Web.App_Start.NinjectWebCommon), "Stop")]

namespace NuGet.Lucene.Web.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    public static class NinjectWebCommon 
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
            
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();
            
            RegisterServices(kernel);
            return kernel;
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            var cfg = new LuceneRepositoryConfigurator
                {
                    EnablePackageFileWatcher = true,
                    LuceneIndexPath = @"D:\Workspace\NuGet.Lucene\SampleData\Lucene",
                    PackagePath = @"D:\Workspace\NuGet.Lucene\SampleData\Packages"
                };

            cfg.Initialize();

            kernel.Bind<LuceneRepositoryConfigurator>().ToConstant(cfg);
            kernel.Bind<ILucenePackageRepository>().ToConstant(cfg.Repository);

            GlobalConfiguration.Configuration.DependencyResolver = new NinjectResolver(kernel);
        }
    }
}
