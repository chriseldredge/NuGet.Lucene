using System.Linq;
using System.Threading.Tasks;
using NuGet.Lucene.Tests;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesODataControllerSearchTests : PackagesODataControllerTestBase
    {
        [Test]
        public void Search()
        {
            var packages = new LucenePackage[0];
            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable()).Verifiable();
            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$orderby=Id");

            controller.Search("foo", "", includePrerelease: false, options: queryOptions);

            repo.VerifyAll();
        }

        [Test]
        public void Search_IgnoresLatestVersionFilter()
        {
            var packages = new[]
            {
                new TestPackage("Foo", "1.0") { SupportedFrameworks = new[] {"net35"}, IsLatestVersion = false }
            };

            repo.Setup(r => r.Search("foo", new[] {"net35"}, false)).Returns(packages.AsQueryable());
            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$take=20&$filter=IsLatestVersion");

            var result = controller.Search("foo", "net35", includePrerelease: false, options: queryOptions);

            Assert.That(result.Select(p => p.Id + "." + p.Version).ToArray(), Is.EquivalentTo(new[] {"Foo.1.0"}));
        }

        [Test]
        public void Search_AllowsOtherFilters()
        {
            var packages = new[]
            {
                new TestPackage("Foo", "1.0") { VersionDownloadCount = 0 },
                new TestPackage("Foo", "2.0") { VersionDownloadCount = 5 }
            };

            repo.Setup(r => r.Search("foo", new[] { "net35" }, false)).Returns(packages.AsQueryable());
            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$filter=VersionDownloadCount gt 1");

            var result = controller.Search("foo", "net35", includePrerelease: false, options: queryOptions);

            Assert.That(result.Select(p => p.Id + "." + p.Version).ToArray(), Is.EquivalentTo(new[] { "Foo.2.0" }));
        }

        [Test]
        public void Search_TranslatesConcatOrderByClause()
        {
            var packages = new[]
            {
                new TestPackage("another.thing", "1.0"),
                new TestPackage("b", "2.0") { Title = "a thing"}
            };

            repo.Setup(r => r.Search("foo", new[] { "net35" }, false)).Returns(packages.AsQueryable());

            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$orderby=concat(Title,Id),Id");

            var result = controller.Search("foo", "net35", includePrerelease: false, options: queryOptions);

            Assert.That(result.Select(p => p.Id).ToArray(), Is.EqualTo(new[] { "b", "another.thing" }));
        }

        [Test]
        public void Search_TranslatesConcatOrderByClause_Desc()
        {
            var packages = new[]
            {
                new TestPackage("another.thing", "1.0"),
                new TestPackage("b", "2.0") { Title = "a thing"}
            };

            repo.Setup(r => r.Search("foo", new[] { "net35" }, false)).Returns(packages.AsQueryable());

            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$orderby=concat(Title,Id)+desc,Id");

            var result = controller.Search("foo", "net35", includePrerelease: false, options: queryOptions);

            Assert.That(result.Select(p => p.Id).ToArray(), Is.EqualTo(new[] { "another.thing", "b" }));
        }

        [Test]
        public async Task CountSearch()
        {
            var packages = new [] { new TestPackage("a", "1.0")};
            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable()).Verifiable();
            var queryOptions = SetUpRequestWithOptions("/api/odata/Search()?$orderby=Id");

            var response = controller.CountSearch("foo", "", false, queryOptions);

            Assert.That(int.Parse(await response.Content.ReadAsStringAsync()), Is.EqualTo(packages.Count()));
        }
    }
}
