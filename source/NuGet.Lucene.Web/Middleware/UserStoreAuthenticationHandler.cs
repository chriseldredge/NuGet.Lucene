namespace NuGet.Lucene.Web.Middleware
{
    public abstract class UserStoreAuthenticationHandler : AuthenticationHandlerBase
    {
        protected readonly UserStore store;

        protected UserStoreAuthenticationHandler(UserStore store)
        {
            this.store = store;
        }
    }
}