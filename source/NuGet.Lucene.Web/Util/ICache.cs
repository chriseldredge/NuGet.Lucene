using System;
using System.Web.Caching;
using System.Web.Hosting;

namespace NuGet.Lucene.Web.Util
{
    public interface ICache
    {
        T Get<T>(string key);
        void Add(string key, object value, TimeSpan timeToLive);
    }

    public class WebCache : ICache
    {
        private readonly Cache cache = HostingEnvironment.Cache;

        public T Get<T>(string key)
        {
            return (T) cache.Get(key);
        }

        public void Add(string key, object value, TimeSpan timeToLive)
        {
            cache.Insert(key, value, null, DateTime.Now.Add(timeToLive), Cache.NoSlidingExpiration);
        }
    }
}