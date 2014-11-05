using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.Lucene
{
    public class FastZipPackage : FastZipPackageBase, IFastZipPackage
    {
        private static readonly ISet<string> PackageFileExcludeExtensions
            = new HashSet<string>{ ".pdscmp", ".psmdcp", ".nuspec", ".rels" };

        protected internal FastZipPackage()
        {
            FrameworkAssemblies = Enumerable.Empty<FrameworkAssemblyReference>();
            DependencySets = Enumerable.Empty<PackageDependencySet>();
            Files = Enumerable.Empty<IPackageFile>();
        }

        private static FastZipPackage Open(string fileLocation, Stream stream, byte[] hash)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream.CanRead must be supported.", "stream");
            }
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream.CanSeek must be supported.", "stream");
            }

            var fastZipPackage = new FastZipPackage { FileLocation = fileLocation };

            using (var package = Package.Open(stream, FileMode.Open, FileAccess.Read))
            {
                fastZipPackage.ProcessManifest(package);

                fastZipPackage.ProcessPackageContents(package);
            }

            stream.Seek(0, SeekOrigin.Begin);

            fastZipPackage.ProcessFileMetadata(stream);

            fastZipPackage.Size = stream.Length;
            fastZipPackage.Hash = hash;

            return fastZipPackage;
        }

        public static FastZipPackage Open(string fileLocation, byte[] hash)
        {
            using (var stream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read))
            {
                return Open(fileLocation, stream, hash);
            }
        }

        public static IFastZipPackage Open(string fileLocation, IHashProvider hashProvider)
        {
            using (var stream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read))
            {
                var hash = hashProvider.CalculateHash(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return Open(fileLocation, stream, hash);
            }
        }

        public override Stream GetStream()
        {
            return new FileStream(FileLocation, FileMode.Open, FileAccess.Read);
        }

        public string GetFileLocation()
        {
            return FileLocation;
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Files;
        }

        public IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return FrameworkAssemblies.SelectMany(f => f.SupportedFrameworks)
                .Union(Files.SelectMany(f => f.SupportedFrameworks))
                .Where(f => f != null && f != VersionUtility.UnsupportedFrameworkName);
        }

        public string Id { get; set; }
        public SemanticVersion Version { get; set; }
        public string FileLocation { get; set; }
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
        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; set; }
        public IEnumerable<PackageDependencySet> DependencySets { get; private set; }
        public Uri ReportAbuseUrl { get { return null; } }
        public int DownloadCount { get { return 0; } }
        public byte[] Hash { get; set; }
        public Version MinClientVersion { get; private set; }
        public bool DevelopmentDependency { get; private set; }
        public IEnumerable<IPackageFile> Files { get; set; }
        public bool IsAbsoluteLatestVersion { get { return false; } }
        public bool IsLatestVersion { get { return false; } }
        public bool Listed { get { return false; } }
        public DateTimeOffset? Published { get { return null; } }
        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { return Enumerable.Empty<IPackageAssemblyReference>(); }
        }
        public ICollection<PackageReferenceSet> PackageAssemblyReferences { get; private set; }
        public DateTimeOffset Created { get; private set; }
        public long Size { get; private set; }

        protected virtual void ProcessPackageContents(Package package)
        {
            Files = package.GetParts()
                .Where(p => !PackageFileExcludeExtensions.Contains(Path.GetExtension(p.Uri.OriginalString)))
                .Select(p => new FastZipPackageFile(this, p.Uri.OriginalString))
                .ToArray();
        }
        
        protected virtual void ProcessFileMetadata(Stream stream)
        {
            Created = GetPackageCreatedDateTime(stream);
        }

        protected virtual void ProcessManifest(Package package)
        {
            var packageRelationship =
                package.GetRelationshipsByType("http://schemas.microsoft.com/packaging/2010/07/manifest")
                .SingleOrDefault();

            if (packageRelationship == null)
            {
                throw new InvalidOperationException("Package does not contain a manifest");
            }

            var part = package.GetPart(packageRelationship.TargetUri);

            ProcessManifest(part.GetStream());
        }

        protected virtual void ProcessManifest(Stream manifestStream)
        {
            var manifest = Manifest.ReadFrom(manifestStream, validateSchema: false);
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
