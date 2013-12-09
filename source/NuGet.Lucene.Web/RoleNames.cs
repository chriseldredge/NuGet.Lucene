using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NuGet.Lucene.Web
{
    public static class RoleNames
    {
        /// <summary>
        /// Allowed to create and delete accounts,
        /// grant and revoke roles,
        /// view and change API keys for all accounts
        /// </summary>
        public const string AccountAdministrator = "AccountAdministrator";

        /// <summary>
        /// Allowed to create, delete or overwrite packages,
        /// explicitly synchronize packages on disk with index
        /// or cancel ongoing synchronization.
        /// </summary>
        public const string PackageManager = "PackageManager";

        /// <summary>
        /// Array of all roles used by NuGet.Lucene.Web.
        /// </summary>
        public static IEnumerable<string> All
        {
            get
            {
                return typeof (RoleNames)
                    .GetFields(BindingFlags.Public | BindingFlags.GetField | BindingFlags.Static)
                    .Select(f => f.GetValue(null))
                    .Cast<string>();
            }
        }
    }
}