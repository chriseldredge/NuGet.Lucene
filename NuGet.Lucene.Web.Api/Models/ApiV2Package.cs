using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.Lucene.Web.Models
{
    //[DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("LastUpdated", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    //[HasStream]
    public class ApiV2Package
    {
        public ApiV2Package(LucenePackage package)
        {
            Id = package.Id;
            Version = package.Version.ToString();
            Title = package.Title;
            Authors = String.Join(",", package.Authors);
            Owners = String.Join(",", package.Owners);
            if (package.IconUrl != null)
            {
                IconUrl = package.IconUrl.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            if (package.LicenseUrl != null)
            {
                LicenseUrl = package.LicenseUrl.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            if (package.ProjectUrl != null)
            {
                ProjectUrl = package.ProjectUrl.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            Description = package.Description;
            Summary = package.Summary;
            ReleaseNotes = package.ReleaseNotes;
            Tags = package.Tags;
            Dependencies = String.Join("|", package.DependencySets.SelectMany(ConvertDependencySetToStrings));
            PackageHash = package.PackageHash;
            PackageHashAlgorithm = "SHA512";
            PackageSize = package.PackageSize;
            LastUpdated = package.LastUpdated.UtcDateTime;
            Published = package.Published.GetValueOrDefault().UtcDateTime;
            Created = package.Created.UtcDateTime;
            IsAbsoluteLatestVersion = package.IsAbsoluteLatestVersion;
            IsLatestVersion = package.IsLatestVersion;
            IsPrerelease = package.IsPrerelease;
            Listed = package.Listed;
            DownloadCount = package.DownloadCount;
            VersionDownloadCount = package.VersionDownloadCount;
            Path = package.Path;
        }

        internal string Path
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }

        public string Version
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Authors
        {
            get;
            set;
        }

        public string Owners
        {
            get;
            set;
        }

        public string IconUrl
        {
            get;
            set;
        }

        public string LicenseUrl
        {
            get;
            set;
        }

        public string ProjectUrl
        {
            get;
            set;
        }

        public int DownloadCount
        {
            get;
            set;
        }

        public bool RequireLicenseAcceptance
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string Summary
        {
            get;
            set;
        }

        public string ReleaseNotes
        {
            get;
            set;
        }

        public DateTime Created
        {
            get;
            set;
        }

        public DateTime Published
        {
            get;
            set;
        }

        public DateTime LastUpdated
        {
            get;
            set;
        }

        public string Dependencies
        {
            get;
            set;
        }

        public string PackageHash
        {
            get;
            set;
        }

        public string PackageHashAlgorithm
        {
            get;
            set;
        }

        public long PackageSize
        {
            get;
            set;
        }

        public string Copyright
        {
            get;
            set;
        }

        public string Tags
        {
            get;
            set;
        }

        public bool IsAbsoluteLatestVersion
        {
            get;
            set;
        }

        public bool IsLatestVersion
        {
            get;
            set;
        }

        public bool IsPrerelease
        {
            get;
            set;
        }

        public bool Listed
        {
            get;
            set;
        }

        public int VersionDownloadCount
        {
            get;
            set;
        }

        public float Score
        {
            get;
            set;
        }

        private IEnumerable<string> ConvertDependencySetToStrings(PackageDependencySet dependencySet)
        {
            if (dependencySet.Dependencies.Count == 0)
            {
                if (dependencySet.TargetFramework != null)
                {
                    // if this Dependency set is empty, we still need to send down one string of the form "::<target framework>",
                    // so that the client can reconstruct an empty group.
                    return new[] { string.Format("::{0}", VersionUtility.GetShortFrameworkName(dependencySet.TargetFramework)) };
                }
            }
            else
            {
                return dependencySet.Dependencies.Select(dependency => ConvertDependency(dependency, dependencySet.TargetFramework));
            }

            return new string[0];
        }

        private string ConvertDependency(PackageDependency packageDependency, FrameworkName targetFramework)
        {
            if (targetFramework != null)
            {
                return String.Format("{0}:{1}:{2}", packageDependency.Id, packageDependency.VersionSpec,
                                     VersionUtility.GetShortFrameworkName(targetFramework));
            }

            if (packageDependency.VersionSpec == null)
            {
                return packageDependency.Id;
            }

            return String.Format("{0}:{1}", packageDependency.Id, packageDependency.VersionSpec);
        }
    }
}