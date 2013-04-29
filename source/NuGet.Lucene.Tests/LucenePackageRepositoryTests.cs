using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class LucenePackageRepositoryTests : TestBase
    {
        private Mock<IPackageIndexer> indexer;
        private LucenePackageRepository repository;

        [SetUp]
        public void SetUp()
        {
            indexer = new Mock<IPackageIndexer>();
            repository = new LucenePackageRepository(packagePathResolver.Object, fileSystem.Object)
                             {
                                 Indexer = indexer.Object,
                                 LucenePackages = datasource,
                                 LuceneDataProvider = provider,
                                 HashProvider = new CryptoHashProvider()
                             };
        }

        [Test]
        public void IncrementDownloadCount()
        {
            var pkg = MakeSamplePackage("sample", "2.1");
            indexer.Setup(i => i.IncrementDownloadCount(pkg)).Verifiable();

            repository.IncrementDownloadCount(pkg);

            indexer.Verify();
        }

        [Test]
        public void Initialize_UpdatesTotalPackages()
        {
            var p = MakeSamplePackage("a", "1.0");
            repository.LucenePackages = new EnumerableQuery<LucenePackage>(Enumerable.Repeat(p, 1234));

            repository.Initialize();

            Assert.That(repository.PackageCount, Is.EqualTo(repository.LucenePackages.Count()));
        }

        [Test]
        public void FindPackage()
        {
            InsertPackage("a", "1.0");
            InsertPackage("a", "2.0");
            InsertPackage("b", "2.0");

            var result = repository.FindPackage("a", new SemanticVersion("2.0"));

            Assert.That(result.Id, Is.EqualTo("a"));
            Assert.That(result.Version.ToString(), Is.EqualTo("2.0"));
        }

        [Test]
        public void FindPackage_ExactMatch()
        {
            InsertPackage("a", "1.0");
            InsertPackage("a", "1.0.0.0");

            var result = repository.FindPackage("a", new SemanticVersion("1.0.0.0"));

            Assert.That(result.Id, Is.EqualTo("a"));
            Assert.That(result.Version.ToString(), Is.EqualTo("1.0.0.0"));
        }

        [Test]
        public void ConvertPackage_TrimsAuthors()
        {
            var package = SetUpConvertPackage();

            package.Object.Authors = new[] {"a", " b"};
            package.Object.Owners = new[] {"c", " d"};

            var result = repository.Convert(package.Object);

            Assert.That(result.Authors.ToArray(), Is.EqualTo(new[] {"a", "b"}));
            Assert.That(result.Owners.ToArray(), Is.EqualTo(new[] { "c", "d" }));
        }

        [Test]
        public void ConvertPackage_SupportedFrameworks()
        {
            var package = SetUpConvertPackage();

            var result = repository.Convert(package.Object);

            Assert.That(result.SupportedFrameworks, Is.Not.Null, "SupportedFrameworks");
            Assert.That(result.SupportedFrameworks.ToArray(), Is.EquivalentTo(new[] {"net40"}));
        }

        [Test]
        public void ConvertPackage_Files()
        {
            var package = SetUpConvertPackage();
            var file1 = new Mock<IPackageFile>();
            file1.Setup(f => f.Path).Returns("path1");
            package.Object.Files = new[] {file1.Object};

            var result = repository.Convert(package.Object);

            Assert.That(result.Files, Is.Not.Null, "Files");
            Assert.That(result.Files.ToArray(), Is.EquivalentTo(new[] { "path1" }));
        }

        [Test]
        public void ConvertPackage_RemovesPlaceholderUrls()
        {
            var package = SetUpConvertPackage();

            package.Object.IconUrl = new Uri("http://ICON_URL_HERE_OR_DELETE_THIS_LINE");
            package.Object.LicenseUrl = new Uri("http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE");
            package.Object.ProjectUrl = new Uri("http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE");

            var result = repository.Convert(package.Object);

            Assert.That(result.IconUrl, Is.Null, "IconUrl");
            Assert.That(result.LicenseUrl, Is.Null, "LicenseUrl");
            Assert.That(result.ProjectUrl, Is.Null, "ProjectUrl");
        }

        [Test]
        public void GetUpdates()
        {
            var a1 = MakeSamplePackage("a", "1.0");
            var a2 = MakeSamplePackage("a", "2.0");
            var a3 = MakeSamplePackage("a", "3.0");

            a3.IsLatestVersion = true;

            InsertPackage(a1);
            InsertPackage(a2);
            InsertPackage(a3);

            var result = repository.GetUpdates(new[] {a1}, false, false, new FrameworkName[0]);

            Assert.That(result.Single().Version.ToString(), Is.EqualTo(a3.Version.ToString()));
        }

        [Test]
        public void GetUpdatesIncludeAll()
        {
            var a1 = MakeSamplePackage("a", "1.0");
            var a2 = MakeSamplePackage("a", "2.0-pre");
            var a3 = MakeSamplePackage("a", "3.0");

            a3.IsLatestVersion = true;

            InsertPackage(a1);
            InsertPackage(a2);
            InsertPackage(a3);

            var result = repository.GetUpdates(new[] { a1 }, true, true, new FrameworkName[0]);

            Assert.That(result.Select(p => p.Version.ToString()).ToArray(), Is.EqualTo(new[] {a2.Version.ToString(), a3.Version.ToString()}));
        }

        private Mock<PackageWithFiles> SetUpConvertPackage()
        {
            var package = new Mock<PackageWithFiles>();

            package.Object.Id = "Sample";
            package.Object.Version = new SemanticVersion("1.0");
            package.Object.DependencySets = new List<PackageDependencySet>();

            package.Setup(p => p.GetSupportedFrameworks()).Returns(new[] {VersionUtility.ParseFrameworkName("net40")});
            fileSystem.Setup(fs => fs.OpenFile(It.IsAny<string>())).Returns(new MemoryStream());
            package.Setup(p => p.GetStream()).Returns(new MemoryStream());
            return package;
        }

        public abstract class PackageWithFiles : LocalPackage
        {
            public IEnumerable<IPackageFile> Files { get;  set; }
            protected sealed override IEnumerable<IPackageFile> GetFilesBase()
            {
                return Files ?? new IPackageFile[0];
            }
        }
    }
}