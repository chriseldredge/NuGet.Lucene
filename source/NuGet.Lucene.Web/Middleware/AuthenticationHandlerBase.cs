using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGet.Lucene.Web.Middleware
{
    public abstract class AuthenticationHandlerBase
    {
        protected readonly Task completedTask;

        private IOwinContext context;

        protected AuthenticationHandlerBase()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetResult(0);
            completedTask = tcs.Task;
        }

        public virtual Task InitializeAsync(IOwinContext context)
        {
            this.context = context;
            return completedTask;
        }

        public virtual Task<bool> InvokeAsync()
        {
            return AuthenticateAsync();
        }

        protected virtual async Task<bool> AuthenticateAsync()
        {
            await AuthenticateCoreAsync();
            return true;
        }

        protected virtual Task AuthenticateCoreAsync()
        {
            return completedTask;
        }

        protected IOwinContext Context
        {
            get { return context; }
        }

        protected IOwinRequest Request
        {
            get { return context.Request; }
        }

        protected IOwinResponse Response
        {
            get { return context.Response; }
        }

        protected bool IsAuthenticated
        {
            get { return Request.User != null && Request.User.Identity.IsAuthenticated; }
        }

        protected string CurrentUsername
        {
            get { return IsAuthenticated ? Request.User.Identity.Name : null; }
        }
    }
}