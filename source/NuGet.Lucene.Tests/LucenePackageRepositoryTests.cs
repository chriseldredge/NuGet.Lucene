using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Linq;
using Moq;
using NuGet.Lucene.IO;
using NUnit.Framework;
using Remotion.Linq.Clauses.ResultOperators;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class LucenePackageRepositoryTests : TestBase
    {
        private Mock<IPackageIndexer> indexer;
        private TestableLucenePackageRepository repository;

        [SetUp]
        public void SetUp()
        {
            indexer = new Mock<IPackageIndexer>();
            repository = new TestableLucenePackageRepository(packagePathResolver.Object, fileSystem.Object)
                             {
                                 Indexer = indexer.Object,
                                 LucenePackages = datasource,
                                 LuceneDataProvider = provider,
                                 HashProvider = new CryptoHashProvider()
                             };
        }

        public class InitializeTests : LucenePackageRepositoryTests
        {
            [Test]
            public void UpdatesTotalPackages()
            {
                var p = MakeSamplePackage("a", "1.0");
                repository.LucenePackages = new EnumerableQuery<LucenePackage>(Enumerable.Repeat(p, 1234));

                repository.Initialize();

                Assert.That(repository.PackageCount, Is.EqualTo(repository.LucenePackages.Count()));
            }
        }

        public class IncrementDownloadCountTests : LucenePackageRepositoryTests
        {
            [Test]
            public async Task IncrementDownloadCount()
            {
                var pkg = MakeSamplePackage("sample", "2.1");
                indexer.Setup(i => i.IncrementDownloadCountAsync(pkg, CancellationToken.None)).Returns(Task.FromResult(true)).Verifiable();

                await repository.IncrementDownloadCountAsync(pkg, CancellationToken.None);

                indexer.Verify();
            }
        }

        public class FindPackageTests : LucenePackageRepositoryTests
        {
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
        }

        public class ConvertPackageTests : LucenePackageRepositoryTests
        {
            [Test]
            public void TrimsAuthors()
            {
                var package = SetUpConvertPackage();

                package.SetupGet(p => p.Authors).Returns(new[] {"a", " b"});
                package.SetupGet(p => p.Owners).Returns(new[] { "c", " d" });

                var result = repository.Convert(package.Object);

                Assert.That(result.Authors.ToArray(), Is.EqualTo(new[] {"a", "b"}));
                Assert.That(result.Owners.ToArray(), Is.EqualTo(new[] {"c", "d"}));
            }

            [Test]
            public void SupportedFrameworks()
            {
                var package = SetUpConvertPackage();
                package.Setup(p => p.GetSupportedFrameworks()).Returns(new[] { VersionUtility.ParseFrameworkName("net40") });

                var result = repository.Convert(package.Object);

                Assert.That(result.SupportedFrameworks, Is.Not.Null, "SupportedFrameworks");
                Assert.That(result.SupportedFrameworks.ToArray(), Is.EquivalentTo(new[] {"net40"}));
            }

            [Test]
            [TestCase("magicsauce", "magicsauce", "0.0", "")]
            [TestCase("magicsauce-lite", "magicsauce", "0.0", "lite")]
            [TestCase("magicsauce12", "magicsauce", "1.2", "")]
            [TestCase("magicsauce123-lite", "magicsauce", "1.2.3", "lite")]
            public void SupportedFrameworks_Custom(string expected, string identifier, string version, string profile)
            {
                var package = SetUpConvertPackage();
                package.Setup(p => p.GetSupportedFrameworks()).Returns(new[] { new FrameworkName(identifier, new Version(version), profile) });

                var result = repository.Convert(package.Object);

                Assert.That(result.SupportedFrameworks, Is.Not.Null, "SupportedFrameworks");
                Assert.That(result.SupportedFrameworks.ToArray(), Is.EquivalentTo(new[] { expected }));
            }

            [Test]
            public void DetectsInvalidModifiedTime()
            {
                var package = SetUpConvertPackage();
                Assert.That(package.Object.Version, Is.Not.Null);
                fileSystem.Setup(fs => fs.GetLastModified(It.IsAny<string>()))
                    .Returns(new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
                Assert.That(package.Object.Version, Is.Not.Null);
                var result = repository.Convert(package.Object);

                Assert.That(result.Published, Is.EqualTo(result.Created));
            }

            [Test]
            public void Files()
            {
                var file1 = new Mock<IPackageFile>();
                file1.SetupGet(f => f.Path).Returns("path1");

                var package = new PackageWithFiles
                {
                    Id = "Sample",
                    Version = new SemanticVersion("1.0"),
                    Files = new[] {file1.Object}
                };

                fileSystem.Setup(fs => fs.OpenFile(It.IsAny<string>())).Returns(new MemoryStream());
                var result = repository.Convert(package);

                Assert.That(result.Files, Is.Not.Null, "Files");
                Assert.That(result.Files.ToArray(), Is.EquivalentTo(new[] {"path1"}));
            }

            [Test]
            public void RemovesPlaceholderUrls()
            {
                var package = SetUpConvertPackage();

                package.SetupGet(p => p.IconUrl).Returns(new Uri("http://ICON_URL_HERE_OR_DELETE_THIS_LINE"));
                package.SetupGet(p => p.LicenseUrl).Returns(new Uri("http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE"));
                package.SetupGet(p => p.ProjectUrl).Returns(new Uri("http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE"));

                var result = repository.Convert(package.Object);

                Assert.That(result.IconUrl, Is.Null, "IconUrl");
                Assert.That(result.LicenseUrl, Is.Null, "LicenseUrl");
                Assert.That(result.ProjectUrl, Is.Null, "ProjectUrl");
            }

            [Test]
            public void SetsPath()
            {
                var package = SetUpConvertPackage();

                var result = repository.Convert(package.Object);

                Assert.That(result.Path, Is.EqualTo(Path.Combine("package-dir", "Sample.1.0")));
            }
        }

        public class GetUpdatesTests : LucenePackageRepositoryTests
        {
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
            public void FilterByVersionConstraint()
            {
                var a1 = MakeSamplePackage("a", "1.0");
                var a2 = MakeSamplePackage("a", "2.0");
                var a3 = MakeSamplePackage("a", "3.0");

                a3.IsLatestVersion = true;

                InsertPackage(a1);
                InsertPackage(a2);
                InsertPackage(a3);

                var constraints = new[] { VersionUtility.ParseVersionSpec("[1.0,2.0]") };
                var result = repository.GetUpdates(new[] { a1 }, false, false, new FrameworkName[0], constraints);

                Assert.That(result.Single().Version.ToString(), Is.EqualTo(a2.Version.ToString()));
            }

            [Test]
            public void FilterByVersionConstraint_ExcludesCurrentVersion()
            {
                var a1 = MakeSamplePackage("a", "1.0");
                var a2 = MakeSamplePackage("a", "2.0");
                var a3 = MakeSamplePackage("a", "3.0");

                a3.IsLatestVersion = true;

                InsertPackage(a1);
                InsertPackage(a2);
                InsertPackage(a3);

                var constraints = new[] { VersionUtility.ParseVersionSpec("[1.0,2.0]") };
                var result = repository.GetUpdates(new[] { a2 }, false, false, new FrameworkName[0], constraints);

                Assert.That(result.ToList(), Is.Empty);
            }

            [Test]
            public void FilterByTargetFrameworkVersion()
            {
                var b1 = MakeSamplePackage("b", "1.0");
                var a1 = MakeSamplePackage("a", "1.0");
                var a2 = MakeSamplePackage("a", "2.0");
                var a3 = MakeSamplePackage("a", "3.0");

                a2.SupportedFrameworks = new[] {"net20"};
                a3.SupportedFrameworks = new[] {"net451"};
                a3.IsLatestVersion = true;

                InsertPackage(b1);
                InsertPackage(a1);
                InsertPackage(a2);
                InsertPackage(a3);

                var result = repository.GetUpdates(new[] {b1, a1}, false, false, a2.GetSupportedFrameworks());

                Assert.That(result.Single().Version.ToString(), Is.EqualTo(a2.Version.ToString()));
            }

            [Test]
            public void IncludeAll()
            {
                var a1 = MakeSamplePackage("a", "1.0");
                var a2 = MakeSamplePackage("a", "2.0-pre");
                var a3 = MakeSamplePackage("a", "3.0");

                a3.IsLatestVersion = true;

                InsertPackage(a1);
                InsertPackage(a2);
                InsertPackage(a3);

                var result = repository.GetUpdates(new[] {a1}, true, true, new FrameworkName[0]);

                Assert.That(result.Select(p => p.Version.ToString()).ToArray(),
                    Is.EqualTo(new[] {a2.Version.ToString(), a3.Version.ToString()}));
            }
        }

        public class SearchTests : LucenePackageRepositoryTests
        {
            [Test]
            public void Id()
            {
                InsertPackage("Foo.Bar", "1.0");

                var result = repository.Search(new SearchCriteria("Foo.Bar"));

                Assert.That(result.Select(r => r.Id).ToArray(), Is.EqualTo(new[] {"Foo.Bar"}));
            }

            [Test]
            public void TokenizeId()
            {
                InsertPackage("Foo.Bar", "1.0");
                InsertPackage("Foo.Baz", "1.0");

                var result = repository.Search(new SearchCriteria("Bar"));

                Assert.That(result.Select(r => r.Id).ToArray(), Is.EqualTo(new[] { "Foo.Bar" }));
            }

            [Test]
            public void TokenizeIdPrefix()
            {
                InsertPackage("Foo.BarBell.ThingUpdater", "1.0");
                InsertPackage("Foo.BarBell.OtherThing", "1.0");

                var result = repository.Search(new SearchCriteria("Foo.BarBell"));
                Assert.That(result.Select(r => r.Id).ToArray(), Is.EquivalentTo(new[] { "Foo.BarBell.ThingUpdater", "Foo.BarBell.OtherThing" }));
            }

            [Test]
            public void TokenizeIdPrefix_LowerCase()
            {
                InsertPackage("Foo.BarBell.ThingUpdater", "1.0");
                InsertPackage("Foo.BarBell.OtherThing", "1.0");

                var result = repository.Search(new SearchCriteria("foo.barbell"));
                Assert.That(result.Select(r => r.Id).ToArray(), Is.EquivalentTo(new[] { "Foo.BarBell.ThingUpdater", "Foo.BarBell.OtherThing" }));
            }

            [Test]
            public void TokenizeIdPrefix_LowerCase_Full()
            {
                InsertPackage("Microsoft.AspNet.Razor", "1.0");

                var result = repository.Search(new SearchCriteria("id:microsoft.aspnet.razor"));
                Assert.That(result.Select(r => r.Id).ToArray(), Is.EquivalentTo(new[] { "Microsoft.AspNet.Razor" }));
            }

            [Test]
            public void FilterOnTargetFramework_ExactMatch()
            {
                var pkg1 = MakeSamplePackage("Foo.Bar", "1.0");
                pkg1.SupportedFrameworks = new[] {"net40"};
                var pkg2 = MakeSamplePackage("Foo.Bar", "2.0");
                pkg2.SupportedFrameworks = new[] { "net45" };

                InsertPackage(pkg1);
                InsertPackage(pkg2);
                repository.Initialize();

                var result = repository.Search(new SearchCriteria("id:Foo.Bar") { TargetFrameworks = new[] {"net40"} });

                Assert.That(result.Select(r => r.Version).ToArray(), Is.EquivalentTo(new[] { pkg1.Version.SemanticVersion }));
            }

            [Test]
            public void FilterOnTargetFramework_ExactMatch_NonStandardFramework()
            {
                var pkg1 = MakeSamplePackage("Foo.Bar", "1.0");
                pkg1.SupportedFrameworks = new[] { "mono38" };
                var pkg2 = MakeSamplePackage("Foo.Bar", "2.0");
                pkg2.SupportedFrameworks = new[] { "net45" };

                InsertPackage(pkg1);
                InsertPackage(pkg2);
                repository.Initialize();

                var result = repository.Search(new SearchCriteria("id:Foo.Bar") { TargetFrameworks = new[] { "mono38" } });

                Assert.That(result.Select(r => r.Version).ToArray(), Is.EquivalentTo(new[] { pkg1.Version.SemanticVersion }));
            }

            [Test]
            public void FilterOnTargetFramework_CompatibleMatch()
            {
                var pkg1 = MakeSamplePackage("Foo.Bar", "1.0");
                pkg1.SupportedFrameworks = new[] { "net20" };
                var pkg2 = MakeSamplePackage("Foo.Bar", "2.0");
                pkg2.SupportedFrameworks = new[] { "net45" };

                InsertPackage(pkg1);
                InsertPackage(pkg2);
                repository.Initialize();

                var result = repository.Search(new SearchCriteria("id:Foo.Bar") { TargetFrameworks = new[] { "net35" } });

                Assert.That(result.Select(r => r.Version).ToArray(), Is.EquivalentTo(new[] { pkg1.Version.SemanticVersion }));
            }

            [Test]
            public void FilterOnTargetFramework_CompatibleMatch_WithProfile()
            {
                var pkg1 = MakeSamplePackage("Foo.Bar", "1.0");
                pkg1.SupportedFrameworks = new[] { "net40-cf" };
                var pkg2 = MakeSamplePackage("Foo.Bar", "2.0");
                pkg2.SupportedFrameworks = new[] { "net45" };

                InsertPackage(pkg1);
                InsertPackage(pkg2);
                repository.Initialize();

                var result = repository.Search(new SearchCriteria("id:Foo.Bar") { TargetFrameworks = new[] { "net40-cf" } });

                Assert.That(result.Select(r => r.Version).ToArray(), Is.EquivalentTo(new[] { pkg1.Version.SemanticVersion }));
            }

            [Test]
            public void FilterOnTargetFramework_Portable_ExactMatch()
            {
                var pkg1 = MakeSamplePackage("Foo.Bar", "1.0");
                pkg1.SupportedFrameworks = new[] { "portable-net40+sl50+wp80+win80" };
                var pkg2 = MakeSamplePackage("Foo.Bar", "2.0");
                pkg2.SupportedFrameworks = new[] { "net45" };

                InsertPackage(pkg1);
                InsertPackage(pkg2);
                repository.Initialize();

                var result = repository.Search(new SearchCriteria("id:Foo.Bar") { TargetFrameworks = new[] { "portable-net40+sl50+wp80+win80" } });

                Assert.That(result.Select(r => r.Version).ToArray(), Is.EquivalentTo(new[] { pkg1.Version.SemanticVersion }));
            }

            [Test]
            public void FilterOnTargetFramework_CompatibleProfile()
            {
                var pkg1 = MakeSamplePackage("Foo.Bar", "1.0");
                pkg1.SupportedFrameworks = new[] { "net40-client" };
                var pkg2 = MakeSamplePackage("Foo.Bar", "2.0");
                pkg2.SupportedFrameworks = new[] { "net45" };

                InsertPackage(pkg1);
                InsertPackage(pkg2);
                repository.Initialize();

                var result = repository.Search(new SearchCriteria("") { TargetFrameworks = new[] { "net40" } });

                Assert.That(result.Select(r => r.Version).ToArray(), Is.EquivalentTo(new[] { pkg1.Version.SemanticVersion }));
            }

            [Test]
            public void FilterOnTargetFramework_AllowsPackagesWithNoSupportedFrameworks()
            {
                var pkg1 = MakeSamplePackage("Foo.Bar", "1.0");
                pkg1.SupportedFrameworks = new string[0];
                var pkg2 = MakeSamplePackage("Yaz.Jazz", "2.0");
                pkg2.SupportedFrameworks = new[] { "net45" };

                InsertPackage(pkg1);
                InsertPackage(pkg2);
                repository.Initialize();

                var result = repository.Search(new SearchCriteria("") { TargetFrameworks = new[] { "net40" } });

                Assert.That(result.Select(r => r.Version).ToArray(), Is.EquivalentTo(new[] { pkg1.Version.SemanticVersion }));
            }

            [Test]
            public void FileName()
            {
                var pkg = MakeSamplePackage("Foo.Bar", "1.0");
                pkg.Files = new[] {"/lib/net45/baz.dll"};
                InsertPackage(pkg);

                var result = repository.Search(new SearchCriteria("baz.dll"));

                Assert.That(result.Select(r => r.Id).ToArray(), Is.EqualTo(new[] { "Foo.Bar" }));
            }

            [Test]
            public void FileNameCaseInsensitive()
            {
                var pkg = MakeSamplePackage("Foo.Bar", "1.0");
                pkg.Files = new[] { "/lib/net45/Baz.DLL" };
                InsertPackage(pkg);

                var result = repository.Search(new SearchCriteria("baz.dll"));

                Assert.That(result.Select(r => r.Id).ToArray(), Is.EqualTo(new[] { "Foo.Bar" }));
            }

            [Test]
            public void FileNameFullPath()
            {
                var pkg = MakeSamplePackage("Foo.Bar", "1.0");
                pkg.Files = new[] { "/lib/net45/baz.dll" };
                InsertPackage(pkg);

                var result = repository.Search(new SearchCriteria("/lib/net45/baz.dll"));

                Assert.That(result.Select(r => r.Id).ToArray(), Is.EqualTo(new[] { "Foo.Bar" }));
            }

            [Test]
            public void FileNameNoExtension()
            {
                var pkg = MakeSamplePackage("Foo.Bar", "1.0");
                pkg.Files = new[] { "/lib/net45/Biz.Baz.DLL" };
                InsertPackage(pkg);

                var result = repository.Search(new SearchCriteria("Biz.Baz"));

                Assert.That(result.Select(r => r.Id).ToArray(), Is.EqualTo(new[] { "Foo.Bar" }));
            }

            [Test]
            public void FileNamePartialMatch()
            {
                var pkg = MakeSamplePackage("Foo.Bar", "1.0");
                pkg.Files = new[] { "/lib/net45/Biz.Baz.DLL" };
                InsertPackage(pkg);

                var result = repository.Search(new SearchCriteria("Baz"));

                Assert.That(result.Select(r => r.Id).ToArray(), Is.EqualTo(new[] { "Foo.Bar" }));
            }

            [Test]
            public void UsesAdvancedWhenColonPresent()
            {
                var query = repository.Search("Tags:remoting", Enumerable.Empty<string>(), allowPrereleaseVersions: true);

                AssertComputedQueryEquals(query, "Tags:remot");
            }

            [Test]
            public void AdvancedQueryCaseInsensitiveField()
            {
                var query = repository.Search("tags:remoting", Enumerable.Empty<string>(), allowPrereleaseVersions: true);

                AssertComputedQueryEquals(query, "Tags:remot");
            }

            [Test]
            public void AdvancedSearchUsesFallbackField()
            {
                var query = repository.Search("NoSuchField:foo", Enumerable.Empty<string>(), allowPrereleaseVersions: true);

                AssertComputedQueryEquals(query, "SearchId:foo");
            }

            [Test]
            public void AdvancedSearchMalformedQueryThrows()
            {
                TestDelegate call = () => repository.Search("(Tags:foo", Enumerable.Empty<string>(), allowPrereleaseVersions: true);

                Assert.That(call, Throws.InstanceOf<InvalidSearchCriteriaException>());
            }

            [Test]
            [Description("See http://docs.nuget.org/docs/reference/search-syntax")]
            public void AdvancedSearch_PackageId_ExactMatch()
            {
                var query = repository.Search("PackageId:Foo.Bar", Enumerable.Empty<string>(), allowPrereleaseVersions: true);

                AssertComputedQueryEquals(query, "Id:foo.bar");
            }

            [Test]
            [Description("See http://docs.nuget.org/docs/reference/search-syntax")]
            public void AdvancedSearch_Id_FuzzyMatch()
            {
                var query = repository.Search("Id:Foo", Enumerable.Empty<string>(), allowPrereleaseVersions: true);

                AssertComputedQueryEquals(query, "SearchId:foo");
            }

            private static void AssertComputedQueryEquals(IQueryable<IPackage> query, string expectedQuery)
            {
                LuceneQueryStatistics stats = null;

                var result = query.CaptureStatistics(s => { stats = s; }).ToArray();

                Assert.That(stats.Query.ToString(), Is.EqualTo(expectedQuery));
            }
        }

        public class LoadFromFileSystemTests : LucenePackageRepositoryTests
        {
            readonly DateTime lastModified = new DateTime(2001, 5, 27, 0, 0, 0, DateTimeKind.Utc);
            readonly string expectedPath = Path.Combine("a", "non", "standard", "package", "location.nupkg");

            [Test]
            public void LoadFromFileSystem()
            {
                SetupFileSystem();

                repository.LoadFromFileSystem(expectedPath);

                fileSystem.Verify();
            }

            [Test]
            public void SetsPublishedDateToLastModified()
            {
                SetupFileSystem();
                fileSystem.Setup(fs => fs.GetLastModified(It.IsAny<string>())).Returns(lastModified);

                var result = repository.LoadFromFileSystem(expectedPath);

                Assert.That(result.Published.GetValueOrDefault().DateTime, Is.EqualTo(lastModified));
            }

            [Test]
            public void SetsPath()
            {
                SetupFileSystem();

                var result = repository.LoadFromFileSystem(expectedPath);

                Assert.That(result.Path, Is.EqualTo(expectedPath));
            }

            [Test]
            public void MakesPathRelative()
            {
                SetupFileSystem();

                var result = repository.LoadFromFileSystem(Path.Combine(Environment.CurrentDirectory, expectedPath));

                Assert.That(result.Path, Is.EqualTo(expectedPath));
            }

            private void SetupFileSystem()
            {
                var root = Environment.CurrentDirectory;

                fileSystem.Setup(fs => fs.Root).Returns(Environment.CurrentDirectory);
                fileSystem.Setup(fs => fs.GetFullPath(It.IsAny<string>()))
                    .Returns<string>(p => Path.Combine(root, p));
                fileSystem.Setup(fs => fs.OpenFile(It.IsAny<string>())).Returns(new MemoryStream());
            }
        }

        public class AddDataServicePackageTests : LucenePackageRepositoryTests
        {
            [SetUp]
            public void SetHashAlgorithm()
            {
                repository.HashAlgorithmName = "SHA256";
            }

            [Test]
            public async Task DownloadAndIndex()
            {
                var package = new FakeDataServicePackage(new Uri("http://example.com/packages/Foo/1.0"));
                fileSystem.Setup(fs => fs.GetFullPath(It.IsAny<string>())).Returns<string>(n => Path.Combine(Environment.CurrentDirectory, n));

                await repository.AddPackageAsync(package, CancellationToken.None);

                indexer.Verify(i => i.AddPackageAsync(It.IsAny<LucenePackage>(), It.IsAny<CancellationToken>()), Times.Once);
            }

            [Test]
            public async Task DownloadAndIndex_DeletesTempFile()
            {
                var package = new FakeDataServicePackage(new Uri("http://example.com/packages/Foo/1.0"));
                fileSystem.Setup(fs => fs.GetFullPath(It.IsAny<string>())).Returns<string>(n => Path.Combine(Environment.CurrentDirectory, n));

                await repository.AddPackageAsync(package, CancellationToken.None);

                fileSystem.Verify(fs => fs.DeleteFile(It.IsRegex(@"\.tmp[\\/].+\.nupkg.tmp")));
            }

            [Test]
            public async Task DenyPackageOverwrite()
            {
                var p = MakeSamplePackage("Foo", "1.0");
                repository.LucenePackages = (new[] {p}).AsQueryable();
                repository.PackageOverwriteMode = PackageOverwriteMode.Deny;

                try
                {
                    await repository.AddPackageAsync(p, CancellationToken.None);
                    Assert.Fail("Expected PackageOverwriteDeniedException");
                }
                catch (PackageOverwriteDeniedException)
                {
                }


                indexer.Verify(i => i.AddPackageAsync(It.IsAny<LucenePackage>(), It.IsAny<CancellationToken>()), Times.Never);
            }

            [Test]
            public async Task CancelDuringGetDownloadStream()
            {
                var package = new FakeDataServicePackage(new Uri("http://example.com/packages/Foo/1.0"));
                packagePathResolver.Setup(r => r.GetInstallPath(package)).Returns("Foo");
                packagePathResolver.Setup(r => r.GetPackageFileName(package)).Returns("Foo.1.0.nupkg");
                var insideHandlerSignal = new ManualResetEventSlim(initialState: false);
                var proceedFromHandlerSignal = new ManualResetEventSlim(initialState: false);
                var exception = new TaskCanceledException("Fake");
                repository.MessageHandler = new FakeHttpMessageHandler((req, token) =>
                {
                    insideHandlerSignal.Set();
                    Assert.True(proceedFromHandlerSignal.Wait(TimeSpan.FromMilliseconds(500)), "Timeout waiting for proceedFromHandlerSignal");
                    if (token.IsCancellationRequested)
                    {
                        throw exception;
                    }
                });

                var cts = new CancellationTokenSource();

                var cancelTask = Task.Run(() =>
                {
                    Assert.True(insideHandlerSignal.Wait(TimeSpan.FromMilliseconds(500)), "Timeout waiting for MessageHandler.SendAsync");
                    cts.Cancel();
                    proceedFromHandlerSignal.Set();
                });

                try
                {
                    await repository.AddPackageAsync(package, cts.Token);
                    await cancelTask;
                    Assert.Fail("Expected TaskCanceledException");
                }
                catch (TaskCanceledException ex)
                {
                    Assert.That(ex, Is.SameAs(exception), "Expected spcific instance of TaskCanceledException");
                }
            }
        }

        private Mock<IPackage> SetUpConvertPackage()
        {
            var package = new Mock<IPackage>().SetupAllProperties();
            package.SetupGet(p => p.Id).Returns("Sample");
            package.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0"));
            package.SetupGet(p => p.DependencySets).Returns(new List<PackageDependencySet>());

            fileSystem.Setup(fs => fs.OpenFile(It.IsAny<string>())).Returns(new MemoryStream());
            package.Setup(p => p.GetStream()).Returns(new MemoryStream());
            Assert.That(package.Object.Version, Is.Not.Null);
            return package;
        }

        public class PackageWithFiles : LocalPackage
        {
            public IEnumerable<IPackageFile> Files { get;  set; }

            public PackageWithFiles()
            {
                DependencySets = Enumerable.Empty<PackageDependencySet>();
            }

            protected sealed override IEnumerable<IPackageFile> GetFilesBase()
            {
                return Files ?? new IPackageFile[0];
            }

            public override Stream GetStream()
            {
                return new MemoryStream();
            }

            protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesCore()
            {
                return Enumerable.Empty<IPackageAssemblyReference>();
            }

            public override IEnumerable<FrameworkName> GetSupportedFrameworks()
            {
                return Enumerable.Empty<FrameworkName>();
            }
        }

        class FakeDataServicePackage : DataServicePackage
        {
            public FakeDataServicePackage(Uri packageStreamUri)
            {
                Id = "Sample";
                Version = "1.0";

                var context = new Mock<IDataServiceContext>();
                context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(packageStreamUri);
                var prop = typeof(DataServicePackage).GetProperty("Context", BindingFlags.NonPublic | BindingFlags.Instance);
                prop.SetValue(this, context.Object);
            }
        }

        public class TestableLucenePackageRepository : LucenePackageRepository
        {
            public HttpMessageHandler MessageHandler { get; set; }

            public TestableLucenePackageRepository(IPackagePathResolver packageResolver, IFileSystem fileSystem)
                : base(packageResolver, fileSystem)
            {
                MessageHandler = new FakeHttpMessageHandler((req, cancel) => {});
            }

            protected override IPackage OpenPackage(string path)
            {
                return new TestPackage(Path.GetFileNameWithoutExtension(path));
            }

            public override IFastZipPackage LoadStagedPackage(HashingWriteStream packageStream)
            {
                packageStream.Close();
                return new FastZipPackage
                {
                    Id = "Sample",
                    Version = new SemanticVersion("1.0"),
                    FileLocation = packageStream.FileLocation,
                    Hash = packageStream.Hash
                };
            }

            protected override Stream OpenFileWriteStream(string path)
            {
                return new MemoryStream();
            }

            protected override void MoveFileWithOverwrite(string src, string dest)
            {
            }

            protected override System.Net.Http.HttpClient CreateHttpClient()
            {
                return new System.Net.Http.HttpClient(MessageHandler);
            }
        }

        public class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Action<HttpRequestMessage, CancellationToken> onSendAsync;

            public FakeHttpMessageHandler(Action<HttpRequestMessage, CancellationToken> onSendAsync)
            {
                this.onSendAsync = onSendAsync;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                onSendAsync(request, cancellationToken);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("the package contents")});
            }
        }
    }
}
