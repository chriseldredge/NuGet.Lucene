using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Linq.Mapping;
using NuGet.Lucene.Mapping;

namespace NuGet.Lucene
{
    /// <summary>
    /// Representation of a NuGet package stored/retrieved in a Lucene index.
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Version = {Version}")]
    public class LucenePackage : IPackage
    {
        private readonly Func<string, Stream> getStream;

        public LucenePackage(IFileSystem fileSystem)
            : this(fileSystem.OpenFile)
        {
        }

        public LucenePackage(Func<string, Stream> getStream)
        {
            this.getStream = getStream;

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

        [Field(Store = StoreMode.No, Analyzer = typeof (PorterStemAnalyzer))]
        public string SearchTitle
        {
            get
            {
                var text = string.IsNullOrWhiteSpace(Title)
                    ? Id
                    : Id + " " + Title;
                text = text.Replace('.', ' ');

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
            return SupportedFrameworks.Select(VersionUtility.ParseFrameworkName);
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Files.Select(path => new LucenePackageFile(path));
        }

        public Stream GetStream()
        {
            return getStream(Path);
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

        public IEnumerable<string> Files { get; set; }
        
        [Field(IndexMode.NotIndexed)]
        public Uri OriginUrl { get; set; }

        public bool IsMirrored { get; set; }
        #endregion
    }

    public class LucenePackageFile : IPackageFile
    {
        private readonly FrameworkName targetFramework;

        public LucenePackageFile(string path)
        {
            Path = path;

            string effectivePath;
            targetFramework = VersionUtility.ParseFrameworkNameFromFilePath(path, out effectivePath);
            EffectivePath = effectivePath;
        }

        public string Path
        {
            get;
            private set;
        }

        public string EffectivePath
        {
            get;
            private set;
        }

        public FrameworkName TargetFramework
        {
            get
            {
                return targetFramework;
            }
        }

        IEnumerable<FrameworkName> IFrameworkTargetable.SupportedFrameworks
        {
            get
            {
                if (TargetFramework != null)
                {
                    yield return TargetFramework;
                }
            }
        }

        public Stream GetStream()
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
