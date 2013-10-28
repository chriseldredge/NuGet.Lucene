using System;
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
            username = username.Replace('/', '\\');

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
        [Authorize(Roles = RoleNames.UserAdmin)]
        public HttpResponseMessage Put(string username, [FromBody]ApiUser user)
        {
            username = username.Replace('/', '\\');

            user.Username = username;

            if (string.IsNullOrWhiteSpace(user.Key))
            {
                user.Key = Guid.NewGuid().ToString();
            }

            using (var session = Provider.OpenSession<ApiUser>())
            {
                session.Add(user);
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        [Authorize(Roles = RoleNames.UserAdmin)]
        public HttpResponseMessage Delete(string username)
        {
            username = username.Replace('/', '\\');

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

        /// <summary>
        /// Retrieves information about the logged-in user
        /// including name and api key. If the session has
        /// not been authenticated, returns a 206 No Content
        /// response with an empty body.
        /// </summary>
        public object GetAuthenticationInfo()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NoContent, "Not authenticated.");
            }

            return GetRequiredAuthenticationInfo();
        }

        /// <summary>
        /// Retrieves information about the logged-in user
        /// including name and api key. If the request has
        /// not been authenticated, this method will force
        /// authentication to occur before retrieving
        /// information.
        /// </summary>
        [Authorize]
        public ApiUser GetRequiredAuthenticationInfo()
        {
            var name = User.Identity.Name;

            var apiUser = Provider.AsQueryable<ApiUser>().SingleOrDefault(u => u.Username == name);

            if (apiUser != null) return apiUser;

            using (var session = Provider.OpenSession<ApiUser>())
            {
                apiUser = new ApiUser { Username = name, Key = Guid.NewGuid().ToString() };
                session.Add(apiUser);
            }

            return apiUser;
        }

        private dynamic DescribeUser(ApiUser user)
        {
            dynamic d = new ExpandoObject();
            
            d.Username = user.Username;

            if (User.IsInRole(RoleNames.UserAdmin) || string.Equals(User.Identity.Name, user.Username, StringComparison.InvariantCultureIgnoreCase))
            {
                d.ApiKey = user.Key;
            }

            return d;
        }
    }
}