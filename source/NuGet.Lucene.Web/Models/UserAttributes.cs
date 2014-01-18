namespace NuGet.Lucene.Web.Models
{
    public class UserAttributes
    {
        public UserAttributes()
        {
            Overwrite = true;
        }

        /// <summary>
        /// API Key to be used by nuget to authenticate as this user.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Roles granted to this user.
        /// </summary>
        public string[] Roles { get; set; }
        
        /// <summary>
        /// When <c>false</c>, prevent overwriting existing users.
        /// Default is <c>true</c>.
        /// </summary>
        public bool Overwrite { get; set; }
    }
}