using System.Collections.Generic;
using System.Web;

namespace NuGet.Lucene.Web.Modules
{
    public abstract class HttpModule : IHttpModule
    {
        private HttpApplication application;

        public void Init(HttpApplication application)
        {
            this.application = application;

            Init();
        }

        protected virtual void Init()
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public HttpApplication Application { get { return application; } }
        public HttpServerUtility Server { get { return application.Server; } }

        public HttpContextBase Context { get { return new HttpContextWrapper(application.Context); } }
        public HttpRequestBase Request { get { return new HttpRequestWrapper(application.Request); } }
        public HttpResponseBase Response { get { return new HttpResponseWrapper(application.Response); } }
    }
}