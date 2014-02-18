using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.Lucene
{
    public class FastZipPackage : IPackage
    {
        private readonly string originalFilePath;

        private FastZipPackage(string originalFilePath)
        {
            this.originalFilePath = originalFilePath;
        }

        public string Id { get; private set; }
        public SemanticVersion Version { get; private set; }
        public string Title { get; private set; }
        public IEnumerable<string> Authors { get; private set; }
        public IEnumerable<string> Owners { get; private set; }
        public Uri IconUrl { get; private set; }
        public Uri LicenseUrl { get; private set; }
        public Uri ProjectUrl { get; private set; }
        public bool RequireLicenseAcceptance { get; private set; }
        public string Description { get; private set; }
        public string Summary { get; private set; }
        public string ReleaseNotes { get; private set; }
        public string Language { get; private set; }
        public string Tags { get; private set; }
        public string Copyright { get; private set; }
        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; private set; }
        public IEnumerable<PackageDependencySet> DependencySets { get; private set; }
        public Uri ReportAbuseUrl { get; private set; }
        public int DownloadCount { get; private set; }
        public byte[] Hash { get; private set; }
        public Version MinClientVersion { get; private set; }
        public bool DevelopmentDependency { get; private set; }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return new IPackageFile[0];
        }

        public IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return new FrameworkName[0];
        }

        public Stream GetStream()
        {
            return new FileStream(originalFilePath, FileMode.Open, FileAccess.Read);
        }

        public bool IsAbsoluteLatestVersion { get; private set; }
        public bool IsLatestVersion { get; private set; }
        public bool Listed { get; private set; }
        public DateTimeOffset? Published { get; private set; }
        public IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; private set; }
        public ICollection<PackageReferenceSet> PackageAssemblyReferences { get; private set; }
        public DateTimeOffset Created { get; private set; }
        public long Size { get; private set; }

        public static FastZipPackage Open(string originalFilePath, IHashProvider hashProvider)
        {
            var result = new FastZipPackage(originalFilePath);

            using (var package = Package.Open(originalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var packageRelationship = package.GetRelationshipsByType("http://schemas.microsoft.com/packaging/2010/07/manifest").SingleOrDefault();
                if (packageRelationship == null)
                    throw new InvalidOperationException("Package does not contain a manifest");
                var part = package.GetPart(packageRelationship.TargetUri);
                using (var stream2 = part.GetStream())
                    result.ReadManifest(stream2);
            }

            using (var stream = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read))
            {
                result.Hash = hashProvider.CalculateHash(stream);
            }

            var info = new FileInfo(originalFilePath);
            result.Created = info.CreationTimeUtc;
            result.Size = info.Length;

            return result;
        }

        protected void ReadManifest(Stream manifestStream)
        {
            var manifest = Manifest.ReadFrom(manifestStream, validateSchema:false);
            var packageMetadata = (IPackageMetadata)manifest.Metadata;
            Id = packageMetadata.Id;
            Version = packageMetadata.Version;
            MinClientVersion = packageMetadata.MinClientVersion;
            Title = packageMetadata.Title;
            Authors = packageMetadata.Authors;
            Owners = packageMetadata.Owners;
            IconUrl = packageMetadata.IconUrl;
            LicenseUrl = packageMetadata.LicenseUrl;
            ProjectUrl = packageMetadata.ProjectUrl;
            RequireLicenseAcceptance = packageMetadata.RequireLicenseAcceptance;
            Description = packageMetadata.Description;
            Summary = packageMetadata.Summary;
            ReleaseNotes = packageMetadata.ReleaseNotes;
            Language = packageMetadata.Language;
            Tags = packageMetadata.Tags;
            DependencySets = packageMetadata.DependencySets;
            FrameworkAssemblies = packageMetadata.FrameworkAssemblies;
            Copyright = packageMetadata.Copyright;
            PackageAssemblyReferences = packageMetadata.PackageAssemblyReferences;
            DevelopmentDependency = packageMetadata.DevelopmentDependency;

            if (string.IsNullOrEmpty(Tags))
                return;
            Tags = " " + Tags + " ";
        }
    }
}