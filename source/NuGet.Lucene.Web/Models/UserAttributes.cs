namespace NuGet.Lucene.Web.Models
{
    public class UserAttributes
    {
        /// <summary>
        /// API Key to be used by nuget to authenticate as this user.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Roles granted to this user.
        /// </summary>
        public string[] Roles { get; set; }
    }
}