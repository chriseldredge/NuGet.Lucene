using System.IO;
using System.Web.Hosting;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiWebHostSettings : NuGetWebApiSettings
    {
        protected override string MapPathFromAppSetting(string key, string defaultValue)
        {
            var path = base.GetAppSetting(key, defaultValue);
            if (string.IsNullOrEmpty(path)) return path;
            if (Path.IsPathRooted(path)) return path;
            return HostingEnvironment.MapPath(path);
        }
    }
}