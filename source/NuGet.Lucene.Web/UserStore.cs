using System.Linq;
using Lucene.Net.Linq;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web
{
    public class UserStore
    {
        private readonly LuceneDataProvider provider;

        public UserStore(LuceneDataProvider provider)
        {
            this.provider = provider;
        }

        public IQueryable<ApiUser> Users
        {
            get
            {
                return provider.AsQueryable<ApiUser>();
            }
        }

        public ISession<ApiUser> OpenSession()
        {
            return provider.OpenSession<ApiUser>();
        }
    }
}