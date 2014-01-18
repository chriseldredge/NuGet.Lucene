using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Web.Http;
using NuGet.Lucene.Web.Authentication;
using NuGet.Lucene.Web.Models;

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
        public UserStore Store { get; set; }

        /// <summary>
        /// Retrieves a list of all users.
        /// </summary>
        public IEnumerable<ApiUser> GetAllUsers()
        {
            return Store.Users.OrderBy(u => u.Username).Select(DescribeUser).ToList();
        }

        /// <summary>
        /// Retrieve information about a user.
        /// </summary>
        public object Get(string username)
        {
            username = ScrubUsername(username);

            var user = Store.Users.SingleOrDefault(u => u.Username == username);

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
        /// <param name="key">API key to set for user (optional). If not specified, a GUID will be generated and used as the key.</param>
        /// <param name="roles">Roles to grant user.</param>
        /// <returns></returns>
        [Authorize(Roles = RoleNames.AccountAdministrator)]
        public HttpResponseMessage Put(string username, [FromBody]UserAttributes attributes)
        {
            username = ScrubUsername(username);

            if (string.IsNullOrWhiteSpace(attributes.Key))
            {
                attributes.Key = Guid.NewGuid().ToString();
            }

            using (var session = Store.OpenSession())
            {
                if (!attributes.Overwrite && session.Query().Any(u => u.Username == username))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Conflict,
                        "User " + username + " already exists.");
                }

                session.Add(new ApiUser{Username = username, Key = attributes.Key, Roles = attributes.Roles});
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing user, optionally renaming.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        [Authorize(Roles = RoleNames.AccountAdministrator)]
        public HttpResponseMessage Post(string username, [FromBody]UpdateUserAttributes attributes)
        {
            username = ScrubUsername(username);
            var renameTo = ScrubUsername(attributes.RenameTo);

            using (var session = Store.OpenSession())
            {
                var user = session.Query().SingleOrDefault(u => u.Username == username);

                if (user == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User" + username + " not found.");
                }

                if (attributes.Key != null)
                {
                    user.Key = attributes.Key;
                }

                if (attributes.Roles != null)
                {
                    user.Roles = attributes.Roles;
                }

                if (attributes.RenameTo == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Created);
                }

                var isRenamingToDifferentName = !renameTo.Equals(username, StringComparison.InvariantCultureIgnoreCase);

                if (isRenamingToDifferentName && !attributes.Overwrite && session.Query().Any(u => u.Username == renameTo))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Conflict,
                        "User " + attributes.RenameTo + " already exists.");
                }

                user.Username = attributes.RenameTo;
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        [Authorize(Roles = RoleNames.AccountAdministrator)]
        public HttpResponseMessage Delete(string username)
        {
            username = ScrubUsername(username);

            using (var session = Store.OpenSession())
            {
                var user = session.Query().SingleOrDefault(u => u.Username == username);

                if (user == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User " + username + " not found.");
                }
                session.Delete(user);
            }

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [Authorize(Roles = RoleNames.AccountAdministrator)]
        public HttpResponseMessage DeleteAllUsers()
        {
            using (var session = Store.OpenSession())
            {
                session.DeleteAll();
            }

            return Request.CreateResponse(HttpStatusCode.NoContent);
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
            var name = ScrubUsername(User.Identity.Name);

            var apiUser = Store.Users.SingleOrDefault(u => u.Username == name);

            if (apiUser != null) return apiUser;

            using (var session = Store.OpenSession())
            {
                apiUser = new ApiUser { Username = name, Key = Guid.NewGuid().ToString(), Roles = GetUserRoles(User) };
                session.Add(apiUser);
            }

            return apiUser;
        }

        /// <summary>
        /// Changes the API key of the authenticated user.
        /// </summary>
        [Authorize]
        [HttpPost]
        public KeyChangeRequest ChangeApiKey([FromBody]KeyChangeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Key))
            {
                req.Key = Guid.NewGuid().ToString();
            }

            using (var session = Store.OpenSession())
            {
                var apiUser = session.Query().Single(u => u.Username == ScrubUsername(User.Identity.Name));
                apiUser.Key = req.Key;
            }

            return req;
        }

        private static string ScrubUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return "";
            return username.Replace('\\', '/');
        }

        private IEnumerable<string> GetUserRoles(IPrincipal user)
        {
            return RoleNames.All.Where(user.IsInRole);
        }

        private ApiUser DescribeUser(ApiUser user)
        {
            if (User.IsInRole(RoleNames.AccountAdministrator) || IsSelf(user))
            {
                return user;
            }

            // Hide details that non-admins should not see.
            return new ApiUser {Username = user.Username};
        }

        private bool IsSelf(ApiUser user)
        {
            return string.Equals(User.Identity.Name, user.Username, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}