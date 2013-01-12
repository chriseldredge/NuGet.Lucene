using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    [TestFixture]
    public class IndexDifferenceCalculatorTests
    {
        private Mock<IFileSystem> fileSystem;
        private static readonly DateTimeOffset SamplePublishedDate = new DateTimeOffset(2012, 5, 29, 13, 42, 23, TimeSpan.Zero);

        [SetUp]
        public void SetUp()
        {
            fileSystem = new Mock<IFileSystem>();
        }

        [Test]
        public void Empty_NoMissingPackages()
        {
            SetupFileSystemPackagePaths();
            var indexedPackages = CreateLucenePackages();

            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.AreEqual(0, diff.MissingPackages.Count());
        }

        [Test]
        public void Empty_NoNewPackages()
        {
            SetupFileSystemPackagePaths();
            var indexedPackages = CreateLucenePackages();

            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.AreEqual(0, diff.NewPackages.Count());
        }

        [Test]
        public void Empty_NoModifiedPackages()
        {
            SetupFileSystemPackagePaths();
            var indexedPackages = CreateLucenePackages();

            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.AreEqual(0, diff.ModifiedPackages.Count());
        }

        [Test]
        public void NewPackages()
        {
            SetupFileSystemPackagePaths("a", "b");
            var indexedPackages = CreateLucenePackages("a");

            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.AreEqual(new[] { "b" }, diff.NewPackages);
        }

        [Test]
        public void MissingPackages()
        {
            SetupFileSystemPackagePaths("b", "c");
            var indexedPackages = CreateLucenePackages("a", "b");

            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.AreEqual(new[] { "a" }, diff.MissingPackages);
        }

        [Test]
        public void UpdatedPackages()
        {
            SetupFileSystemPackagePaths("a", "b");
            var indexedPackages = CreateLucenePackages("a", "b").ToList();
            indexedPackages[0].Published = SamplePublishedDate;
            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.AreEqual(new[] { "b" }, diff.ModifiedPackages);
        }

        [Test]
        public void CaseInsensitive()
        {
            SetupFileSystemPackagePaths("a");
            var indexedPackages = CreateLucenePackages("A").ToList();
            indexedPackages[0].Published = SamplePublishedDate;
            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.That(diff.NewPackages, Is.Empty);
            Assert.That(diff.ModifiedPackages, Is.Empty);
        }

        [Test]
        public void DoesNotCheckFileModifiedTimeWhenPackagePublishedDateNull()
        {
            SetupFileSystemPackagePaths("a");
            var indexedPackages = CreateLucenePackages("a").ToList();
            indexedPackages[0].Published = null;
            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.That(diff.ModifiedPackages.Count(), Is.EqualTo(1));

            fileSystem.Verify(fs => fs.GetLastModified(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void CheckTimeStampOnceEach()
        {
            SetupFileSystemPackagePaths("a");
            var indexedPackages = CreateLucenePackages("a").ToList();
            indexedPackages[0].Published = SamplePublishedDate;
            var diff = IndexDifferenceCalculator.FindDifferences(fileSystem.Object, indexedPackages, CancellationToken.None);

            Assert.That(diff.ModifiedPackages.Count(), Is.EqualTo(0));
            Assert.That(diff.ModifiedPackages.Count(), Is.EqualTo(0));

            fileSystem.Verify(fs => fs.GetLastModified(It.IsAny<string>()), Times.Once());
        }

        private IEnumerable<LucenePackage> CreateLucenePackages(params string[] paths)
        {
            foreach (var p in paths)
            {
                yield return new LucenePackage(fileSystem.Object) { Path = p };
            }
        }

        private void SetupFileSystemPackagePaths(params string[] paths)
        {
            fileSystem.Setup(fs => fs.GetLastModified(It.IsAny<string>())).Returns(SamplePublishedDate.ToLocalTime).Verifiable();
            fileSystem.Setup(fs => fs.GetFiles(string.Empty, "*" + Constants.PackageExtension, true)).Returns(paths).Verifiable();
        }
    }
}