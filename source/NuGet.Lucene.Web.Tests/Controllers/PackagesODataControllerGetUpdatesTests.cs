using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Web.Http.Results;
using Moq;
using NuGet.Lucene.Web.Models;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesODataControllerGetUpdatesTests : PackagesODataControllerTestBase
    {
        [Test]
        public void GetUpdates()
        {
            var packages = new[] {new LucenePackage(path => null)  {Id = "id1", Version = new StrictSemanticVersion("2.0")}};

            var constraint = new VersionSpec {MinVersion = new SemanticVersion("1.0"), IsMinInclusive = false};
            repo.Setup(r => r.GetUpdates(
                It.Is<IEnumerable<IPackage>>(p => p.Count() == 1),
                false,
                false,
                It.Is<IEnumerable<FrameworkName>>(p => !p.Any()),
                It.Is<IEnumerable<IVersionSpec>>(p => p.Single().ToString() == constraint.ToString())))
                .Returns(packages);

            var result = controller.GetUpdates("id1", "1.0", false, false, "", "");

            repo.VerifyAll();

            Assert.That(result, Is.InstanceOf<OkNegotiatedContentResult<IQueryable<ODataPackage>>>());
            var queryable = ((OkNegotiatedContentResult<IQueryable<ODataPackage>>) result).Content;

            Assert.That(queryable.Count(), Is.EqualTo(1));
            Assert.That(queryable.First().Id, Is.EqualTo("id1"));
            Assert.That(queryable.First().Version, Is.EqualTo("2.0"));
        }

        [Test]
        public void GetUpdatesWithVersionConstraint()
        {
            var packages = new[] { new LucenePackage(path => null) { Id = "id1", Version = new StrictSemanticVersion("1.8") } };

            repo.Setup(r => r.GetUpdates(
                It.Is<IEnumerable<IPackage>>(p => p.Count() == 1),
                false,
                false,
                It.Is<IEnumerable<FrameworkName>>(p => !p.Any()),
                It.Is<IEnumerable<IVersionSpec>>(p => p.Single().ToString() == VersionUtility.ParseVersionSpec("[1.0, 2.0)").ToString())))
                .Returns(packages);

            var result = controller.GetUpdates("id1", "1.0", false, false, "", "[1.0,2.0)");

            repo.VerifyAll();

            Assert.That(result, Is.InstanceOf<OkNegotiatedContentResult<IQueryable<ODataPackage>>>());
            var queryable = ((OkNegotiatedContentResult<IQueryable<ODataPackage>>) result).Content;

            Assert.That(queryable.Count(), Is.EqualTo(1));
            Assert.That(queryable.First().Id, Is.EqualTo("id1"));
            Assert.That(queryable.First().Version, Is.EqualTo("1.8"));
        }

        [Test]
        public void MultiplePackages()
        {
            var packages = new[] { new LucenePackage(path => null) { Id = "id1", Version = new StrictSemanticVersion("2.0") } };

            repo.Setup(r => r.GetUpdates(It.Is<IEnumerable<IPackage>>(p => p.Count() == 2), true, true, new FrameworkName[0], It.IsAny<IEnumerable<IVersionSpec>>()))
                .Returns(packages);

            var result = controller.GetUpdates("id1|id2", "1.0|1.5", true, true, "", "");

            repo.VerifyAll();

            Assert.That(result, Is.InstanceOf<OkNegotiatedContentResult<IQueryable<ODataPackage>>>());
            var queryable = ((OkNegotiatedContentResult<IQueryable<ODataPackage>>) result).Content;

            Assert.That(queryable.Count(), Is.EqualTo(1));
            Assert.That(queryable.First().Id, Is.EqualTo("id1"));
            Assert.That(queryable.First().Version, Is.EqualTo("2.0"));
        }

        [Test]
        public void AllowsEmptyVersionConstraints()
        {
            var packages = new[] { new LucenePackage(path => null) { Id = "id1", Version = new StrictSemanticVersion("2.0") } };

            repo.Setup(r => r.GetUpdates(It.Is<IEnumerable<IPackage>>(p => p.Count() == 2), true, true, new FrameworkName[0], It.IsAny<IEnumerable<IVersionSpec>>()))
                .Returns(packages);

            var result = controller.GetUpdates("id1|id2", "1.0|1.5", true, true, "", "|");

            repo.VerifyAll();

            Assert.That(result, Is.InstanceOf<OkNegotiatedContentResult<IQueryable<ODataPackage>>>());
            var queryable = ((OkNegotiatedContentResult<IQueryable<ODataPackage>>)result).Content;

            Assert.That(queryable.Count(), Is.EqualTo(1));
            Assert.That(queryable.First().Id, Is.EqualTo("id1"));
            Assert.That(queryable.First().Version, Is.EqualTo("2.0"));
        }

        [Test]
        public void IgnoresUnbalancedIdsAndVersions()
        {
            var result = controller.GetUpdates("id1|id2", "1.0", false, false, "", "");

            Assert.That(result, Is.InstanceOf<BadRequestErrorMessageResult>());

            repo.Verify(r => r.GetUpdates(It.IsAny<IEnumerable<IPackage>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IEnumerable<FrameworkName>>(), Enumerable.Empty<IVersionSpec>()), Times.Never());
        }

        [Test]
        public void IgnoresEmptyIdsAndVersions()
        {
            var result = controller.GetUpdates("", "", false, false, "", "");

            Assert.That(result, Is.InstanceOf<BadRequestErrorMessageResult>());

            repo.Verify(r => r.GetUpdates(It.IsAny<IEnumerable<IPackage>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IEnumerable<FrameworkName>>(), Enumerable.Empty<IVersionSpec>()), Times.Never());
        }
    }
}
