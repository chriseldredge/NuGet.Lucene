using System;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Lucene.Web.Util
{
    public static class TaskUtils
    {
        /// <summary>
        /// This method enables a Task to be started from asp.net 4.5 without requiring an async method to await it.
        /// </summary>
        /// <remarks>
        ///     When executing an async method in asp.net 4.5 associated with a request and failing to wait for the task
        ///     to complete will result in an exception like the following to be thrown.
        ///     <code>
        ///         System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task
        ///         or accessing its Exception property. As a result, the unobserved exception was rethrown by the
        ///         finalizer thread. ---> System.NullReferenceException: Object reference not set to an instance of an object.
        ///            at System.Web.ThreadContext.AssociateWithCurrentThread(Boolean setImpersonationContext)
        ///            at System.Web.HttpApplication.OnThreadEnterPrivate(Boolean setImpersonationContext)
        ///            at System.Web.HttpApplication.System.Web.Util.ISyncContext.Enter()
        ///            at System.Web.Util.SynchronizationHelper.SafeWrapCallback(Action action)
        ///            at System.Threading.Tasks.Task.Execute()
        ///            --- End of inner exception stack trace ---
        ///         ---> (Inner Exception #0) System.NullReferenceException: Object reference not set to an instance of an object.
        ///            at System.Web.ThreadContext.AssociateWithCurrentThread(Boolean setImpersonationContext)
        ///            at System.Web.HttpApplication.OnThreadEnterPrivate(Boolean setImpersonationContext)
        ///            at System.Web.HttpApplication.System.Web.Util.ISyncContext.Enter()
        ///            at System.Web.Util.SynchronizationHelper.SafeWrapCallback(Action action)
        ///            at System.Threading.Tasks.Task.Execute()
        ///     </code>
        /// </remarks>
        public static void FireAndForget(Func<Task> action, Action<Exception> errorHandler)
        {
            ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        action().Wait();
                    }
                    catch (AggregateException ex)
                    {
                        ex.Handle(e => { errorHandler(e); return true; });
                    }
                    catch (Exception ex)
                    {
                        errorHandler(ex);
                    }
                });
        }
    }
}