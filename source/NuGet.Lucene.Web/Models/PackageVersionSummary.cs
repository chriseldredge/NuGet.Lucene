using System;
using AspNet.WebApi.HtmlMicrodataFormatter;

namespace NuGet.Lucene.Web.Models
{
    public class PackageVersionSummary
    {
        private readonly string id;
        private readonly string title;
        private readonly StrictSemanticVersion version;
        private readonly DateTimeOffset lastUpdated;
        private readonly int versionDownloadCount;
        private readonly Link link;

        public string Id
        {
            get { return id; }
        }

        public string Title
        {
            get { return title; }
        }

        public StrictSemanticVersion Version
        {
            get { return version; }
        }

        public DateTimeOffset LastUpdated
        {
            get { return lastUpdated; }
        }

        public int VersionDownloadCount
        {
            get { return versionDownloadCount; }
        }

        public Link Link
        {
            get { return link; }
        }

        public PackageVersionSummary(LucenePackage package, Link link)
            : this(package.Id, package.Title, package.Version, package.LastUpdated, package.VersionDownloadCount, link)
        {
            
        }

        public PackageVersionSummary(string id, string title, StrictSemanticVersion version, DateTimeOffset lastUpdated, int versionDownloadCount, Link link)
        {
            this.id = id;
            this.title = title;
            this.version = version;
            this.lastUpdated = lastUpdated;
            this.versionDownloadCount = versionDownloadCount;
            this.link = link;
        }
    }
}