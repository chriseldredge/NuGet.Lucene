using System.IO;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    class LucenePackageTests
    {
        LucenePackage package;

        [SetUp]
        public void SetUp()
        {
            package = new LucenePackage(_ => new MemoryStream());
        }

        [Test]
        public void SupportedFrameworks_Unsupported()
        {
            package.SupportedFrameworks = new[] {"flashy_new_thing"};

            Assert.That(package.GetSupportedFrameworks(), Is.EqualTo(new[] {VersionUtility.UnsupportedFrameworkName}));
        }

        [Test]
        public void SupportedFrameworks_Unsupported_Distinct()
        {
            package.SupportedFrameworks = new[] { "flashy_new_thing", "net35", "other_new_thing" };

            Assert.That(package.GetSupportedFrameworks(), Is.EquivalentTo(new[] { VersionUtility.UnsupportedFrameworkName, VersionUtility.ParseFrameworkName("net35") }));
        }
    }
}
