using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using NuGet.Lucene.Web.Util;

namespace NuGet.Lucene.Web.Controllers
{
    public class IndexingController : ApiController
    {
        public ILucenePackageRepository Repository { get; set; }
        public ReusableCancellationTokenSource CancellationTokenSource { get; set; }

        public Action<Func<Task>, Action<Exception>> FireAndForget = TaskUtils.FireAndForget;

        [HttpGet]
        public RepositoryInfo Status()
        {
            return Repository.GetStatus();
        }

        [HttpPost]
        public HttpResponseMessage Synchronize()
        {
            if (Repository.GetStatus().SynchronizationState != SynchronizationState.Idle)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, "Synchronization is already in progress.");
            }

            FireAndForget(() => Repository.SynchronizeWithFileSystem(
                                    CancellationTokenSource.Token),
                                    UnhandledExceptionLogger.LogException);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        public HttpResponseMessage Cancel()
        {
            CancellationTokenSource.Cancel();

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}