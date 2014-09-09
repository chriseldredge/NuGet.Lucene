using System;
using Common.Logging;
using Microsoft.Owin.Hosting;
using Owin;

namespace NuGet.Lucene.Web.OwinHost.Sample
{
    class Program
    {
        private Startup startup;

        static void Main()
        {
            new Program().Run();
        }

        private void Run()
        {
            var log = LogManager.GetCurrentClassLogger();
            const string baseAddress = "http://*:9001/";

            try
            {
                using (WebApp.Start(baseAddress, Start))
                {
                    Console.WriteLine("Listening on " + baseAddress + ". Press <ctrl>+c to stop listening.");
                    Console.WriteLine("Press enter to stop.");
                    Console.ReadLine();
                }

                startup.WaitForShutdown(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                log.Fatal(m => m(ex.Message), ex);
                Console.WriteLine("Press enter to quit.");
                Console.ReadLine();
            }
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        private void Start(IAppBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                LogManager.GetLogger<Program>().Info(m => m("{0} {1}", ctx.Request.Method, ctx.Request.Uri));
                await next();
            });

            startup = new Startup();
            startup.Configuration(app);

        }
    }
}
