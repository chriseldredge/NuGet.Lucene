using System.IO;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class GroupingPackagePathResolverTests : TestBase
    {
        [Test]
        public void GroupsByIdButNotVersion()
        {
            var resolver = new GroupingPackagePathResolver("base", true);

            var result = resolver.GetPackageDirectory("Sample", new SemanticVersion("2.1"));

            Assert.That(result, Is.EqualTo("Sample"));
        }

        [Test]
        public void GetInstallPath()
        {
            var resolver = new GroupingPackagePathResolver("base", true);

            var result = resolver.GetInstallPath(MakeSamplePackage("Sample", "3.5"));

            Assert.That(result, Is.EqualTo(Path.Combine("base", "Sample")));
        }

        [Test]
        public void GetInstallPath_NoGrouping()
        {
            var resolver = new GroupingPackagePathResolver("base", false);

            var result = resolver.GetInstallPath(MakeSamplePackage("Sample", "3.5"));

            Assert.That(result, Is.EqualTo("base"));
        }

        [Test]
        public void IncludesVersionInFileName_IdAndVersion()
        {
            var resolver = new GroupingPackagePathResolver("base", true);

            var result = resolver.GetPackageFileName("Sample", new SemanticVersion("1.0"));

            Assert.That(result, Is.EqualTo("Sample.1.0.nupkg"));
        }

        [Test]
        public void IncludesVersionInFileName_IPackage()
        {
            var resolver = new GroupingPackagePathResolver("base", true);

            var result = resolver.GetPackageFileName(MakeSamplePackage("Sample", "1.0"));

            Assert.That(result, Is.EqualTo("Sample.1.0.nupkg"));
        }
    }
}