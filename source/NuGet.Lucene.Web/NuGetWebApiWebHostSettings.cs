using System.Web.Hosting;

namespace NuGet.Lucene.Web
{
    public class NuGetWebApiWebHostSettings : NuGetWebApiSettings
    {
        protected override string MapPathFromAppSetting(string key, string defaultValue)
        {
            var value = base.MapPathFromAppSetting(key, defaultValue);
            return HostingEnvironment.MapPath(value);
        }
    }
}