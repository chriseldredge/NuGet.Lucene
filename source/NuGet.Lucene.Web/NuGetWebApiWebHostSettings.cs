using System.Collections.Specialized;
using System.IO;
using System.Web.Hosting;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiWebHostSettings : NuGetWebApiSettings
    {
        public NuGetWebApiWebHostSettings(string prefix) : base(prefix)
        {
        }

        public NuGetWebApiWebHostSettings(string prefix, NameValueCollection settings, NameValueCollection roleMappings) : base(prefix, settings, roleMappings)
        {
        }

        protected override string MapPathFromAppSetting(string key, string defaultValue)
        {
            var path = base.GetAppSetting(key, defaultValue);
            if (string.IsNullOrEmpty(path)) return path;
            if (Path.IsPathRooted(path)) return path;
            return HostingEnvironment.MapPath(path);
        }
    }
}
