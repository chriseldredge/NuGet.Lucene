using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Common.Logging;

namespace NuGet.Lucene.Web.Util
{
    public class TaskRunner : ITaskRunner
    {
        private static readonly ILog Log = LogManager.GetLogger<TaskRunner>();
        private readonly ISet<Task> pendingTasks = new HashSet<Task>();

        public CancellationToken ShutdownToken { get; set; }

        /// <summary>
        /// This method enables a Task to be started from asp.net 4.5 without requiring an async method to await it.
        /// </summary>
        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> action)
        {
            var invoker = new Invoker(this, action);

            if (HostingEnvironment.IsHosted)
            {
                QueueuHostedTask(invoker.Invoke);
            }
            else
            {
                Task.Run(() => invoker.Invoke(ShutdownToken));
            }
        }

        public Task[] PendingTasks
        {
            get
            {
                lock (pendingTasks)
                {
                    return pendingTasks.ToArray();
                }
            }
        }

        /// <summary>
        /// Uses reflection to invoke HostingEnvironment.QueueBackgroundWorkItem when
        /// running on ASP.NET 4.5.2 or later or falls back to ThreadPool.QueueUserWorkItem
        /// on older runtimes.
        /// </summary>
        private void QueueuHostedTask(Func<CancellationToken, Task> workItem)
        {
            var method = typeof(HostingEnvironment).GetMethod(
                "QueueBackgroundWorkItem",
                BindingFlags.Static | BindingFlags.Public,
                null,
                CallingConventions.Standard,
                new[] { typeof(Func<CancellationToken, Task>) },
                null);

            if (method != null)
            {
                method.Invoke( /* static */ null, new object[] { workItem });
            }
            else
            {
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    await workItem(ShutdownToken);
                });
            }
        }

        private void AddPendingTask(Task<int> task)
        {
            lock (pendingTasks)
            {
                pendingTasks.Add(task);
            }
        }

        private void RemovePendingTask(Task<int> task)
        {
            lock (pendingTasks)
            {
                pendingTasks.Remove(task);
            }
        }

        private class Invoker
        {
            private readonly TaskRunner outer;
            private readonly Func<CancellationToken, Task> action;

            public Invoker(TaskRunner outer, Func<CancellationToken, Task> action)
            {
                this.action = action;
                this.outer = outer;
            }

            public async Task Invoke(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var tcs = new TaskCompletionSource<int>();

                outer.AddPendingTask(tcs.Task);

                try
                {
                    await action(token);
                }
                catch (OperationCanceledException ex)
                {
                    Log.Warn(m => m("An operation was canceled by request."), ex);
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.All(ie => ie is OperationCanceledException))
                    {
                        Log.Warn(m => m("An operation was canceled by request."), ex);
                    }
                    else
                    {
                        Log.Error(m => m("Unhandled exception in background task."), ex);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(m => m("Unhandled exception in background task."), ex);
                }
                finally
                {
                    outer.RemovePendingTask(tcs.Task);
                    tcs.SetResult(0);
                }
            }
        }
    }
}
