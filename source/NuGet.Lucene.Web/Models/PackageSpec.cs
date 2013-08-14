using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AspNet.WebApi.HtmlMicrodataFormatter;

namespace NuGet.Lucene.Web.Models
{
    public class PackageWithVersionHistory : LucenePackage
    {
        public Link PackageDownloadLink { get; set; }
        public IEnumerable<PackageVersionSummary> VersionHistory { get; set; }

        public PackageWithVersionHistory()
            :base(p => null)
        {
        }
    }

    public class PackageSpec : LocalPackage
    {
        public PackageSpec()
        {
        }

        public PackageSpec(string id, string version)
        {
            Id = id;
            Version = new SemanticVersion(version);
        }

        public override Stream GetStream()
        {
            throw new NotSupportedException();
        }

        protected override IEnumerable<IPackageFile> GetFilesBase()
        {
            throw new NotSupportedException();
        }

        protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesBase()
        {
            throw new NotSupportedException();
        }
    }
}