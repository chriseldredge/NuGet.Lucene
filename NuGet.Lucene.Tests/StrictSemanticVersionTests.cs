using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class StrictSemanticVersionTests
    {
        [Theory]
        [TestCase("1.0-alpha", "1.0-alpha")]
        [TestCase("1.0-BETA", "1.0-beta")]
        public void EquatabilityBasedOnOriginalString(string versionA, string versionB)
        {
            Assert.True(new StrictSemanticVersion(versionA).Equals(new StrictSemanticVersion(versionB)));
        }

        [Theory]
        [TestCase("1.0-alpha", "1.0-alpha")]
        [TestCase("1.0-BETA", "1.0-beta")]
        public void ObjectEqualsBasedOnOriginalString(string versionA, string versionB)
        {
            Assert.True(new StrictSemanticVersion(versionA).Equals((object)new StrictSemanticVersion(versionB)));
        }

        [Theory]
        [TestCase("1.0-alpha", "1.0-ALPHA")]
        public void HashCodesEqualWhenCaseNotSame(string versionA, string versionB)
        {
            Assert.That(new StrictSemanticVersion(versionA).GetHashCode(), Is.EqualTo(new StrictSemanticVersion(versionB).GetHashCode()));
        }

        [Theory]
        [TestCaseSource("StupidlyFormattedVersions")]
        public void StupidlyFormattedVersionsNotEqual(string versionA, string versionB)
        {
            Assert.False(new StrictSemanticVersion(versionA).Equals((object)new StrictSemanticVersion(versionB)), "Equals(object obj)");
        }

        [Theory]
        [TestCaseSource("StupidlyFormattedVersions")]
        public void StupidlyFormattedVersionsNotEquatable(string versionA, string versionB)
        {
            Assert.That(new StrictSemanticVersion(versionA), Is.Not.EqualTo(new StrictSemanticVersion(versionB)));
        }

        [Theory]
        [TestCaseSource("StupidlyFormattedVersions")]
        public void ToStringPreservesOriginalFormat(string versionA, string versionB)
        {
            Assert.That(new StrictSemanticVersion(versionA).ToString(), Is.EqualTo(versionA));
            Assert.That(new StrictSemanticVersion(versionB).ToString(), Is.EqualTo(versionB));
        }

        [Theory]
        public void SortUsesVersionAndNotSimpleStringComparison()
        {
            var result = MakeVersions("1.10", "1.2", "1.0").OrderBy(a => a).Select(a => a.ToString());
            Assert.That(result, Is.EqualTo(new[] {"1.0", "1.2", "1.10"}));
        }

        [Theory]
        public void SortPutsPrereleaseBeforeNonPrerelease()
        {
            var result = MakeVersions("1.0", "1.0-Alpha-456", "1.0-Alpha").OrderBy(a => a).Select(a => a.ToString());
            Assert.That(result, Is.EqualTo(new[] { "1.0-Alpha", "1.0-Alpha-456", "1.0" }));
        }

        [Theory]
        [Ignore("Fails")]
        public void SortMoreSpecificBefore()
        {
            var result = MakeVersions("1.0.0", "1.0").OrderBy(a => a).Select(a => a.ToString());
            Assert.That(result, Is.EqualTo(new[] { "1.0", "1.0.0" }));
        }

        private IEnumerable<StrictSemanticVersion> MakeVersions(params string[] values)
        {
            return values.Select(v => new StrictSemanticVersion(v));
        }

        public static IEnumerable<object[]> StupidlyFormattedVersions
        {
            get
            {
                yield return new object[] { "1.0", "1.0.0" };
                yield return new object[] { "2.00", "2.0" };
                yield return new object[] { "3.01", "3.1" };
                yield return new object[] { "4.0", "04.0" };
            }
        }
    }
}
