using System.Threading;
using Microsoft.Owin;
using Owin;

namespace NuGet.Lucene.Web.Util
{
    public static class OwinAppBuilderExtensions
    {
        public static CancellationToken GetHostAppDisposing(this IAppBuilder appBuilder)
        {
            var context = new OwinContext(appBuilder.Properties);
            return context.Get<CancellationToken>("host.OnAppDisposing");
        }
    }
}
