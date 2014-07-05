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

            var cancelToken = new CancellationTokenSource();

            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += (_, __) => cancelToken.Cancel();

            try
            {
                using (WebApp.Start<Startup>(baseAddress))
                {
                    Console.WriteLine("Listening on " + baseAddress + ". Press <ctrl>+c to stop listening.");
                    cancelToken.Token.WaitHandle.WaitOne();
                }
            }
            catch (Exception ex)
            {
                log.Fatal(m => m(ex.Message), ex);
            }

            Console.WriteLine("Press enter to quit.");
            Console.ReadLine();
        }
    }
}
