using Microsoft.Owin;

namespace NuGet.Lucene.Web.Util
{
    public static class OwinRequestExtensions
    {
        public static bool IsLocal(this IOwinRequest request)
        {
            return request.Get<bool>("server.IsLocal");
        }
    }
}
