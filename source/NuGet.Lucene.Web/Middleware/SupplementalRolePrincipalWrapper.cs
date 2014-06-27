using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace NuGet.Lucene.Web.Middleware
{
    public class SupplementalRolePrincipalWrapper : IPrincipal
    {
        private readonly IPrincipal target;
        private readonly ISet<string> supplementalRoles;

        public SupplementalRolePrincipalWrapper(IPrincipal target, IEnumerable<string> supplementalRoles)
        {
            this.target = target;
            this.supplementalRoles = new HashSet<string>(supplementalRoles, StringComparer.InvariantCultureIgnoreCase);
        }

        public bool IsInRole(string role)
        {
            return supplementalRoles.Contains(role) || target.IsInRole(role);
        }

        public IIdentity Identity
        {
            get { return target.Identity; }
        }
    }
}