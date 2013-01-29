using Lucene.Net.Linq.Mapping;

namespace NuGet.Lucene.Web.Authentication
{
    public class ApiUser
    {
        [Field(Key = true)]
        public string Username { get; set; }
        public string Key { get; set; }
    }
}