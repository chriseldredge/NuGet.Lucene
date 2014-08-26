using System;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Lucene.Web.Util
{
    public interface ITaskRunner
    {
        CancellationToken ShutdownToken { get; set; }
        Task[] PendingTasks { get; }

        /// <summary>
        /// This method enables a Task to be started from asp.net 4.5 without requiring an async method to await it.
        /// When not running in asp.net, falls back to <see cref="Task.Run(System.Action)"/>.
        /// </summary>
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> action);
    }
}
