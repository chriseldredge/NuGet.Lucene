using System;
using System.IO;
using System.Runtime.Versioning;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    class FastZipPackageFileTests
    {
        private readonly IFastZipPackage package = new FastZipPackage();

        [Test]
        public void DeterminesSupportedFrameworkFromLib()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "net20", "Foo.dll"));

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.ParseFrameworkName("net20")));
        }

        [Test]
        public void DeterminesSupportedFrameworkFromTools()
        {
            var file = new FastZipPackageFile(package, Path.Combine("tools", "net45", "Foo.dll"));

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.ParseFrameworkName("net45")));
        }

        [Test]
        public void DeterminesSupportedFrameworkFromBuild()
        {
            var file = new FastZipPackageFile(package, Path.Combine("build", "net40", "Foo.dll"));

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.ParseFrameworkName("net40")));
        }

        [Test]
        public void DeterminesSupportedFrameworkFromContent()
        {
            var file = new FastZipPackageFile(package, Path.Combine("content", "net40-cf", "Foo.config"));

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.ParseFrameworkName("net40-cf")));
        }

        [Test]
        public void EffectivePathRelativeToFrameworkFolder()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "net40", "Foo.dll"));

            Assert.That(file.EffectivePath, Is.EqualTo("Foo.dll"));
        }

        [Test]
        public void EffectivePathDefault()
        {
            var file = new FastZipPackageFile(package, Path.Combine("stuff", "things"));

            Assert.That(file.EffectivePath, Is.EqualTo(file.Path));
        }

        [Test]
        public void IgnoresNonWhiteListFolder()
        {
            var file = new FastZipPackageFile(package, Path.Combine("not-a-thing", "net40", "Foo.config"));

            Assert.That(file.TargetFramework, Is.Null);
        }

        [Test]
        public void NormalizeZipFilePath()
        {
            var file = new FastZipPackageFile(package, @"/lib/net35/Foo.dll");

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.ParseFrameworkName("net35")));
        }

        [Test]
        public void UrlDecodesFilePath()
        {
            var file = new FastZipPackageFile(package, "lib/portable-windows8%2Bnet45/Neato.dll");

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.ParseFrameworkName("portable-windows8+net45")));
        }

        [Test]
        public void ParseUnrecognizedFramework_SingleVersion()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java7", "Foo.jar"));

            Assert.That(file.TargetFramework, Is.EqualTo(new FrameworkName("java", new Version("7.0"))));
        }

        [Test]
        public void RemembersParseFrameworkName()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java7", "Foo.jar"));

            var first = file.TargetFramework;
            Assert.That(file.TargetFramework, Is.EqualTo(first));
        }

        [Test]
        public void ParseUnrecognizedFramework_MultidigitVersion()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java712", "Foo.jar"));

            Assert.That(file.TargetFramework, Is.EqualTo(new FrameworkName("java", new Version("7.1.2"))));
        }

        [Test]
        public void ParseUnrecognizedFramework_SillyLongVersion()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java7123456789", "Foo.jar"));

            Assert.That(file.TargetFramework, Is.EqualTo(new FrameworkName("java", new Version("7.1.2.3456789"))));
        }

        [Test]
        public void ParseUnrecognizedFramework_MultidigitVersion_Profile()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java71-micro", "Foo.jar"));

            Assert.That(file.TargetFramework, Is.EqualTo(new FrameworkName("java", new Version("7.1"), "micro")));
        }

        [Test]
        public void ParseUnrecognizedFramework_MultidigitVersion_Profile_NoHyphen_Unsupported()
        {
            // Castle.ActiveRecord 3.0.0.1 has this invalid library folder:
            var file = new FastZipPackageFile(package, Path.Combine("lib", "net40cp", "Castle.ActiveRecord.dll"));

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.UnsupportedFrameworkName));
        }

        [Test]
        public void UriEscapedSpaces()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", ".NetFramework%204.0", "Spackle.dll"));

            Assert.That(file.TargetFramework, Is.EqualTo(VersionUtility.ParseFrameworkName("net40")));
        }

        [Test]
        public void ParseUnrecognizedFramework_NoVersion()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java", "Foo.jar"));

            Assert.That(file.TargetFramework, Is.EqualTo(new FrameworkName("java", new Version("0.0"))));
        }

        [Test]
        public void ParseUnrecognizedFramework_NoVersion_WithProfile()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java-micro", "Foo.jar"));

            Assert.That(file.TargetFramework, Is.EqualTo(new FrameworkName("java", new Version("0.0"), "micro")));
        }

        [Test]
        public void EffectivePathForUnrecognizedFramework()
        {
            var file = new FastZipPackageFile(package, Path.Combine("lib", "java7", "Foo.jar"));

            Assert.That(file.EffectivePath, Is.EqualTo("Foo.jar"));
        }
    }
}
