using System;
using System.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.DataServices
{
    [TestFixture]
    public class PackageDataServiceSearchTests : PackageDataServiceTestBase
    {
        [Test]
        public void SearchSortsByScore()
        {
            var packages = new LucenePackage[0];

            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable());

            var query = service.Search("foo", "", includePrerelease: false);

            AssertOrderingBy(query, "result => result.Score()");
        }

        [Test]
        public void SearchNoSortWhenSpecifiedInQueryString()
        {
            service.FakeRequestUri = new Uri("http://localhost/packages?$orderby=DownloadCount");
            var packages = new LucenePackage[0];

            repo.Setup(r => r.Search("foo", new string[0], false)).Returns(packages.AsQueryable());

            var query = service.Search("foo", "", includePrerelease: false);

            AssertOrderingBy(query);
        }
    }
}
