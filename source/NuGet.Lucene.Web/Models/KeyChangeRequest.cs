namespace NuGet.Lucene.Web.Models
{
    public class KeyChangeRequest
    {
        public KeyChangeRequest()
        {
        }

        public KeyChangeRequest(string key)
        {
            this.Key = key;
        }

        /// <summary>
        /// The new key to set. If blank, one wil be generated.
        /// </summary>
        public string Key { get; set; }
    }
}