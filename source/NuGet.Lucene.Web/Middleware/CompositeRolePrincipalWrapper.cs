using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace NuGet.Lucene.Web.Middleware
{
    public class CompositeRolePrincipalWrapper : IPrincipal
    {
        private readonly IEnumerable<IPrincipal> targets;

        public CompositeRolePrincipalWrapper(params IPrincipal[] targets)
        {
            if (targets == null)
            {
                throw new ArgumentNullException("targets");
            }
            if (targets.Length < 1)
            {
                throw new ArgumentException("Must provide at least one target IPrincipal", "targets");
            }

            this.targets = targets;
        }

        public bool IsInRole(string role)
        {
            return targets.Any(t => t.IsInRole(role));
        }

        public IIdentity Identity
        {
            get { return targets.First().Identity; }
        }
    }
}
