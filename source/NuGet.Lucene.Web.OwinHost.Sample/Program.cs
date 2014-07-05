using System;
using Common.Logging;
using Microsoft.Owin.Hosting;
using System.Threading;

namespace NuGet.Lucene.Web.OwinHost.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = LogManager.GetCurrentClassLogger();
            const string baseAddress = "http://*:9001/";

            try
            {
                using (WebApp.Start<Startup>(baseAddress))
                {
                    Console.WriteLine("Listening on " + baseAddress + ". Press <ctrl>+c to stop listening.");
                    Console.WriteLine("Press enter to stop.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                log.Fatal(m => m(ex.Message), ex);
                Console.WriteLine("Press enter to quit.");
                Console.ReadLine();
            }
        }
    }
}
