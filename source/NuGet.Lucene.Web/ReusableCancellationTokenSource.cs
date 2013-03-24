using System.Threading;

namespace NuGet.Lucene.Web
{
    public class ReusableCancellationTokenSource
    {
        private readonly object sync = new object();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public CancellationToken Token
        {
            get { lock(sync) { return cancellationTokenSource.Token; } }
        }

        public void Cancel()
        {
            lock (sync)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
            }
        }
    }
}