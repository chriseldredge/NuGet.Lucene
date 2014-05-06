using System;
using Microsoft.Owin.Hosting;
using System.Threading;

namespace NuGet.Lucene.Web.OwinHost.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            const string baseAddress = "http://localhost:9000/";

            var cancelToken = new CancellationTokenSource();

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
                WriteExceptionChain(ex);
            }
            Console.WriteLine("Press enter to quit.");
            Console.ReadLine();
        }

        private static void WriteExceptionChain(Exception ex)
        {
            while (ex != null)
            {
                Console.Error.WriteLine("{0}: {1}", ex.GetType().FullName, ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                ex = ex.InnerException;
            }
        }
    }
}
