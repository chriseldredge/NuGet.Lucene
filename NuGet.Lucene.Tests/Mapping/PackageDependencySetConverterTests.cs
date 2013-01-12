using System.Linq;
using System.Runtime.Versioning;
using NUnit.Framework;
using NuGet.Lucene.Mapping;

namespace NuGet.Lucene.Tests.Mapping
{
    [TestFixture]
    public class PackageDependencySetConverterTests
    {
        private readonly PackageDependency NoConstraint =
            new PackageDependency("id1");
        private readonly PackageDependency ExactVersion =
            new PackageDependency("id2", new VersionSpec(new SemanticVersion("1.0")));

        [Test]
        public void Flatten()
        {
            var frameworkName = VersionUtility.ParseFrameworkName("net35");
            var actual = PackageDependencySetConverter.Flatten(new PackageDependencySet(frameworkName, new[] { NoConstraint, ExactVersion}));

            Assert.AreEqual(new[] { "id1::net35", "id2:[1.0]:net35" }, actual);
        }

        [Test]
        public void Flatten_NoDependenciesForFramework()
        {
            var frameworkName = VersionUtility.ParseFrameworkName("net35");
            var actual = PackageDependencySetConverter.Flatten(new PackageDependencySet(frameworkName, new PackageDependency[0]));

            Assert.AreEqual(new[] { "::net35" }, actual);
        }

        [Test]
        public void Flatten_TargetFrameworkNull()
        {
            var actual = PackageDependencySetConverter.Flatten(new PackageDependencySet(null, new[] { NoConstraint, ExactVersion }));

            Assert.AreEqual(new[] { "id1::", "id2:[1.0]:" }, actual);
        }

        [Test]
        public void ToDependencySets()
        {
            var results = PackageDependencySetConverter.Parse(new[] { "id1", "id3::net35", "id2:1.0", "id4:[1.1,2.0):net20" }).ToList();

            Assert.AreEqual(3, results.Count());

            results.Sort((a, b) => System.String.Compare(a.TargetFramework.FullNameOrBlank(), b.TargetFramework.FullNameOrBlank(), System.StringComparison.Ordinal));

            Assert.Null(results[0].TargetFramework);
            Assert.AreEqual(new[] { "id1", "id2" }, results[0].Dependencies.Select(d => d.Id));
            Assert.That(results[0].Dependencies.Select(d => d.VersionSpec != null ? d.VersionSpec.ToString() : null), Is.EqualTo(new[] { null, "1.0" }));

            Assert.AreEqual("net20", VersionUtility.GetShortFrameworkName(results[1].TargetFramework));
            Assert.AreEqual(new[] { "id4" }, results[1].Dependencies.Select(d => d.Id));
            Assert.That(results[1].Dependencies.Select(d => d.VersionSpec != null ? d.VersionSpec.ToString() : null), Is.EqualTo(new[] { "[1.1, 2.0)" }));

            Assert.AreEqual("net35", VersionUtility.GetShortFrameworkName(results[2].TargetFramework));
            Assert.AreEqual(new[] { "id3" }, results[2].Dependencies.Select(d => d.Id));
            Assert.AreEqual(new IVersionSpec[] { null }, results[2].Dependencies.Select(d => d.VersionSpec));
        }

        [Test]
        public void ToDependencySets_EmptySet()
        {
            var results = PackageDependencySetConverter.Parse(new[] { "::net40" }).ToList();

            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("net40", VersionUtility.GetShortFrameworkName(results[0].TargetFramework));
            Assert.AreEqual(0, results[0].Dependencies.Count());
        }
    }

    static class Helper
    {
        public static string FullNameOrBlank(this FrameworkName frameworkName)
        {
            return frameworkName == null ? "" : frameworkName.FullName;
        }
    }
}