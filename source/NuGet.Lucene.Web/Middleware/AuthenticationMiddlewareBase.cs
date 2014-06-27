using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGet.Lucene.Web.Middleware
{
    public abstract class AuthenticationMiddlewareBase : OwinMiddleware
    {
        protected AuthenticationMiddlewareBase(OwinMiddleware next) : base(next)
        {
        }

        protected abstract AuthenticationHandlerBase CreateHandler();

        public override async Task Invoke(IOwinContext context)
        {
            var handler = CreateHandler();
            await handler.InitializeAsync(context);

            if (await handler.InvokeAsync())
            {
                await Next.Invoke(context);
            }
        }
    }
}