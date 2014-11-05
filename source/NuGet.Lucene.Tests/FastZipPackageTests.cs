using System.Linq;
using System.Runtime.Versioning;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    class FastZipPackageTests
    {
        static readonly FrameworkName FrameworkNet35 = VersionUtility.ParseFrameworkName("net35");
        static readonly FrameworkName FrameworkNet40 = VersionUtility.ParseFrameworkName("net40");

        [Test]
        public void GetsSupportedFrameworksFromMetadata()
        {
            var package = new FastZipPackage
            {
                FrameworkAssemblies = new[] { new FrameworkAssemblyReference("Neato", new[] {FrameworkNet40}) }
            };

            Assert.That(package.GetSupportedFrameworks(), Is.EquivalentTo(new[] {FrameworkNet40}));
        }

        [Test]
        public void FiltersUnsupportedFramework()
        {
            var package = new FastZipPackage
            {
                FrameworkAssemblies = new[] { new FrameworkAssemblyReference("Neato", new[] { VersionUtility.UnsupportedFrameworkName }) }
            };

            Assert.That(package.GetSupportedFrameworks(), Is.Empty);
        }

        [Test]
        public void FiltersNull()
        {
            var package = new FastZipPackage
            {
                FrameworkAssemblies = new[] { new FrameworkAssemblyReference("Neato", new FrameworkName[] { null }) }
            };

            Assert.That(package.GetSupportedFrameworks(), Is.Empty);
        }

        [Test]
        public void GetsSupportedFrameworksFromFiles()
        {
            var package = new FastZipPackage();
            package.Files = new[] {new FastZipPackageFile(package, "lib/net35/Neato.dll")};

            Assert.That(package.GetSupportedFrameworks(), Is.EquivalentTo(new[] { FrameworkNet35 }));
        }

        [Test]
        public void Combines()
        {
            var package = new FastZipPackage();
            package.Files = new[]
            {
                new FastZipPackageFile(package, "lib/net35/Neato.dll"),
            };

            package.FrameworkAssemblies = new[]
            {
                new FrameworkAssemblyReference("Neato", new[] {FrameworkNet40})
            };

            Assert.That(package.GetSupportedFrameworks().ToArray(), Is.EquivalentTo(new[] { FrameworkNet35, FrameworkNet40 }));
        }

        [Test]
        public void Distinct()
        {
            var package = new FastZipPackage();
            package.Files = new[]
            {
                new FastZipPackageFile(package, "lib/net40/Neato.dll")
            };

            package.FrameworkAssemblies = new[]
            {
                new FrameworkAssemblyReference("Neato", new[] {FrameworkNet40})
            };

            Assert.That(package.GetSupportedFrameworks().ToArray(), Is.EqualTo(new[] { FrameworkNet40 }));
        }
    }
}
