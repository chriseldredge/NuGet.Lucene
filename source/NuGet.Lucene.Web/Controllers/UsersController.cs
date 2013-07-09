using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Lucene.Net.Linq;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// <para>
    /// Provides methods to view, create and delete users and api keys for authentication.
    /// </para>
    /// <para>
    /// Clients can authenticate using api keys by setting the <c>X-NuGet-ApiKey</c> header
    /// on requests.
    /// </para>
    /// </summary>
    public class UsersController : ApiController
    {
        public LuceneDataProvider Provider { get; set; }

        public IEnumerable<dynamic> GetAllUsers()
        {
            return Provider.AsQueryable<ApiUser>()
                .Select(DescribeUser)
                .ToList();
        }

        /// <summary>
        /// Retrieve information about a user.
        /// </summary>
        public dynamic Get(string username)
        {
            var user = Provider.AsQueryable<ApiUser>()
                .SingleOrDefault(u => u.Username == username);

            if (user == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User " + username + " not found.");
            }

            return DescribeUser(user);
        }

        /// <summary>
        /// Creates or replaces a user.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public HttpResponseMessage Put(string username, [FromBody]ApiUser user)
        {
            user.Username = username;

            using (var session = Provider.OpenSession<ApiUser>())
            {
                session.Add(user);
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        public HttpResponseMessage Delete(string username)
        {
            using (var session = Provider.OpenSession<ApiUser>())
            {
                var user = session.Query().SingleOrDefault(u => u.Username == username);

                if (user == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User " + username + " not found.");
                }
                session.Delete(user);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private dynamic DescribeUser(ApiUser user)
        {
            dynamic d = new ExpandoObject();
            d.Username = user.Username;

            // if current user is admin
            // d.ApiKey = user.ApiKey

            return d;
        }
    }
}