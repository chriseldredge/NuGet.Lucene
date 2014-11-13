using System.Net;
using System.Net.Http;
using System.Web.Http;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// <para>
    /// Shows current status for indexing activity and provides methods to
    /// manually start the synchronization process or cancel a process in flight.
    /// </para>
    /// <para>
    /// The synchronization process keeps the Lucene index in sync with NuGet
    /// packages on disk by comparing package file modification times with those
    /// found in the Lucene index. Any changes that are detected are reconciled
    /// treating the package files as the authoritative data source.
    /// </para>
    /// <para>
    /// Configuration settings found in Web.config control whether synchronization
    /// should start automatically when the application starts and can also enable
    /// an optional file system watcher to observe changes to package files in real
    /// time and keep the index up to date.
    /// </para>
    /// </summary>
    public class IndexingController : ApiController
    {
        public ILucenePackageRepository Repository { get; set; }
        public StopSynchronizationCancellationTokenSource UserRequestedCancellationTokenSource { get; set; }
        public ITaskRunner TaskRunner { get; set; }

        /// <summary>
        /// Retrieve information about current activity as well as some statistics
        /// such as total number of packages in the repository.
        /// </summary>
        [HttpGet]
        public RepositoryInfo Status()
        {
            return Repository.GetStatus();
        }

        /// <summary>
        /// Starts a new synchronization process if one is not already running.
        /// This method will return immediately instead of waiting for the
        /// synchronization to complete.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> when a new process is accepted and started.
        /// <c>409 Conflict</c> when a process is already running.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = RoleNames.PackageManager)]
        public HttpResponseMessage Synchronize()
        {
            if (Repository.GetStatus().SynchronizationState != SynchronizationState.Idle)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, "Synchronization is already in progress.");
            }
            
            TaskRunner.QueueBackgroundWorkItem(async cancellationToken =>
            {
                var registration = cancellationToken.Register(
                    UserRequestedCancellationTokenSource.Cancel);

                using (registration)
                {
                    await Repository.SynchronizeWithFileSystem(
                        UserRequestedCancellationTokenSource.Token);
                }
            });

            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Cancels a running synchronization process. This method makes no
        /// attempt to cancel a specific synchronization process or even
        /// verify that one is running. This method requests cancelation
        /// but returns immediately without waiting until process has
        /// actually ended.
        /// </summary>
        /// <returns><c>200 OK</c></returns>
        [HttpPost]
        [Authorize(Roles = RoleNames.PackageManager)]
        public HttpResponseMessage Cancel()
        {
            UserRequestedCancellationTokenSource.Cancel();

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// Requests that the Lucene index be optimized, forcing all segments
        /// to be merged and expunging deleted documents.
        /// </summary>
        /// <returns><c>200 OK</c></returns>
        [HttpPost]
        [Authorize(Roles = RoleNames.PackageManager)]
        public HttpResponseMessage Optimize()
        {
            Repository.Optimize();

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
