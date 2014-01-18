namespace NuGet.Lucene.Web.Models
{
    public class UpdateUserAttributes : UserAttributes
    {
        /// <summary>
        /// When set, requests that a user is renamed to this value.
        /// </summary>
        public string RenameTo { get; set; }
    }
}