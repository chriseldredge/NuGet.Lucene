using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Lucene.Net.Linq;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Controllers
{
    public class UserController : ApiController
    {
        public LuceneDataProvider Provider { get; set; }

        public IEnumerable<ApiUser> GetAllUsers()
        {
            return Provider.AsQueryable<ApiUser>().ToList();
        }

        public ApiUser Get(string username)
        {
            return Provider.AsQueryable<ApiUser>().SingleOrDefault(u => u.Username == username);
        }

        public HttpResponseMessage Put(string username, [FromBody]ApiUser user)
        {
            user.Username = username;

            using (var session = Provider.OpenSession<ApiUser>())
            {
                session.Add(user);
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }
    }
}