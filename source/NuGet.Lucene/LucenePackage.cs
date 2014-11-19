using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Linq.Mapping;
using NuGet.Lucene.Analysis;
using NuGet.Lucene.Mapping;

namespace NuGet.Lucene
{
    /// <summary>
    /// Representation of a NuGet package stored/retrieved in a Lucene index.
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Version = {Version}")]
    public class LucenePackage : FastZipPackageBase, IFastZipPackage
    {
        private readonly Func<string, Stream> getStream;
        private readonly Func<string, string> getFullPath;

        public LucenePackage(IFileSystem fileSystem)
            : this(fileSystem.OpenFile, fileSystem.GetFullPath)
        {
        }

        public LucenePackage(Func<string, Stream> getStream)
            :this(getStream, null)
        {
        }

        public LucenePackage(Func<string, Stream> getStream, Func<string, string> getFullPath)
        {
            this.getStream = getStream;
            this.getFullPath = getFullPath;

            Listed = true;

            Authors = Enumerable.Empty<string>();
            Owners = Enumerable.Empty<string>();
            AssemblyReferences = Enumerable.Empty<IPackageAssemblyReference>();
            FrameworkAssemblies = Enumerable.Empty<FrameworkAssemblyReference>();
            Dependencies = Enumerable.Empty<string>();
            Files = Enumerable.Empty<string>();
        }

        [QueryScore]
        public float Score { get; set; }

        #region IPackage

        [Field(Key = true)]
        public string Id { get; set; }

        [IgnoreField]
        SemanticVersion IPackageName.Version
        {
            get { return Version != null ? Version.SemanticVersion : null; }
        }

        [Field("Version", Key = true, Converter = typeof(CachingSemanticVersionConverter))]
        public StrictSemanticVersion Version { get; set; }

        [Field("MinClientVersion", Converter = typeof(CachingVersionConverter))]
        public Version MinClientVersion { get; set; }

        public string Title { get; set; }

        [Field(IndexMode.NotIndexed)]
        public Uri IconUrl { get; set; }

        [Field(IndexMode.NotIndexed)]
        public Uri LicenseUrl { get; set; }

        [Field(IndexMode.NotIndexed)]
        public Uri ProjectUrl { get; set; }

        [Field(IndexMode.NotIndexed)]
        public Uri ReportAbuseUrl { get; set; }

        [NumericField(Converter = typeof(BoolToIntConverter))]
        public bool RequireLicenseAcceptance { get; set; }

        [Field(Store = StoreMode.No, Analyzer = typeof (StandardAnalyzer))]
        public string SearchId
        {
            get
            {
                var text = Id.Replace('.', ' ');

                // Convert "PascalCase" or "camelCase" to "pascal case" or "camel case".
                text = Regex.Replace(text, @"(?<a>(?<!^)((?:[A-Z][a-z])|(?:(?<!^[A-Z]+)[A-Z0-9]+(?:(?=[A-Z][a-z])|$))|(?:[0-9]+)))", @" ${a}");
                return text;
            }
        }

        [Field(Store = StoreMode.No, Analyzer = typeof(PorterStemAnalyzer))]
        public string SearchTitle
        {
            get
            {
                var text = (Title ?? "").Replace('.', ' ');

                // Convert "PascalCase" or "camelCase" to "pascal case" or "camel case".
                text = Regex.Replace(text, @"(?<a>(?<!^)((?:[A-Z][a-z])|(?:(?<!^[A-Z]+)[A-Z0-9]+(?:(?=[A-Z][a-z])|$))|(?:[0-9]+)))", @" ${a}");
                return text;
            }
        }

        [Field(Analyzer = typeof(PorterStemAnalyzer))]
        public string Description { get; set; }

        [Field(Analyzer = typeof(PorterStemAnalyzer))]
        public string Summary { get; set; }

        [Field(Analyzer = typeof(PorterStemAnalyzer))]
        public string ReleaseNotes { get; set; }

        [Field(IndexMode.NotIndexed, Store = StoreMode.Yes)]
        public string Language { get; set; }

        [Field(Analyzer = typeof(PorterStemAnalyzer))]
        public string Tags { get; set; }

        [Field(IndexMode.NotIndexed)]
        public string Copyright { get; set; }

        [NumericField]
        public int DownloadCount { get; set; }

        [NumericField]
        public int VersionDownloadCount { get; set; }

        [NumericField(Converter = typeof(BoolToIntConverter))]
        public bool IsAbsoluteLatestVersion { get; set; }

        [NumericField(Converter = typeof(BoolToIntConverter))]
        public bool IsLatestVersion { get; set; }

        [NumericField(Converter = typeof(BoolToIntConverter))]
        public bool DevelopmentDependency { get; set; }

        [IgnoreField]
        public bool Listed { get; set; }

        [NumericField(Converter = typeof(BoolToIntConverter))]
        public bool IsPrerelease
        {
            get { return !this.IsReleaseVersion(); }
        }

        [NumericField(Converter = typeof(DateTimeOffsetToTicksConverter))]
        public DateTimeOffset? Published { get; set; }

        [Field(Analyzer = typeof(StandardAnalyzer))]
        public IEnumerable<string> Authors { get; set; }

        [Field(Analyzer = typeof(StandardAnalyzer))]
        public IEnumerable<string> Owners { get; set; }

        [Field(Analyzer = typeof(DependencyAnalyzer))]
        public IEnumerable<string> Dependencies { get; set; }

        [IgnoreField]
        public IEnumerable<PackageDependencySet> DependencySets
        {
            get { return PackageDependencySetConverter.Parse(Dependencies); }
            set { Dependencies = value.SelectMany(PackageDependencySetConverter.Flatten); }
        }

        [IgnoreField]
        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; set; }
        
        [IgnoreField]
        public IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; set; }

        [IgnoreField]
        public ICollection<PackageReferenceSet> PackageAssemblyReferences { get; set; }

        public IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return SupportedFrameworks.Select(VersionUtility.ParseFrameworkName).Distinct();
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Files.Select(path => new FastZipPackageFile(this, path));
        }

        public override Stream GetStream()
        {
            return getStream(Path);
        }

        public string GetFileLocation()
        {
            if (getFullPath == null || string.IsNullOrEmpty(Path))
            {
                throw new InvalidOperationException("File lcoation is unknown");
            }
            return getFullPath(Path);
        }

        #endregion

        #region Package metadata

        [NumericField]
        public long PackageSize { get; set; }

        [Field(IndexMode.NotIndexed)]
        public string PackageHash { get; set; }

        [Field(IndexMode.NotIndexed)]
        public string PackageHashAlgorithm { get; set; }

        [NumericField(Converter = typeof(DateTimeOffsetToTicksConverter))]
        public DateTimeOffset LastUpdated { get; set; }

        [NumericField(Converter = typeof(DateTimeOffsetToTicksConverter))]
        public DateTimeOffset Created { get; set; }

        [Field(IndexMode.NotAnalyzed)]
        public string Path { get; set; }

        public IEnumerable<string> SupportedFrameworks { get; set; }

        [Field(Analyzer = typeof(PathAnalyzer))]
        public IEnumerable<string> Files { get; set; }
        
        [Field(IndexMode.NotIndexed)]
        public Uri OriginUrl { get; set; }

        [Field(Analyzer = typeof(BoolNormalizingAnalyzer))]
        public bool IsMirrored { get; set; }
        #endregion
    }
}
