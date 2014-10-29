using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Models;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class TabCompletionControllerTests
    {
        private  TabCompletionController controller;
        private  Mock<ILucenePackageRepository> repository;
        private  Mock<IMirroringPackageRepository> mirroringRepository;
        private  List<LucenePackage> packages;

        [SetUp]
        public void SetUp()
        {
            packages = new List<LucenePackage>();
            repository = new Mock<ILucenePackageRepository>();
            mirroringRepository = new Mock<IMirroringPackageRepository>();
            controller = new TabCompletionController { Repository = repository.Object, MirroringRepository = mirroringRepository.Object };

            repository.Setup(repo => repo.LucenePackages).Returns(packages.AsQueryable());

            mirroringRepository.Setup(repo => repo.FindPackagesById(It.IsAny<string>()))
                .Returns((string id) => packages.Where(p => p.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)));
        }

        [Test]
        public void GetMatchingPackages_NoPackages()
        {
            var result = controller.GetMatchingPackages(null, false, 30);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetMatchingPackages_LimitsTo30Distinct()
        {
            var packageIds = Enumerable.Range(0, 100).Select(i => "Foo-" + i.ToString("D3")).ToList();

            Enumerable.Range(1, 5).ToList().ForEach(version => packageIds.ForEach(i => AddSamplePackage(i, version + ".0")));

            var result = controller.GetMatchingPackages("F", false, 30);

            Assert.That(result, Is.EqualTo(packageIds.Take(30)));
        }

        [Test]
        public void GetMatchingPackages_ExcludePrerelease()
        {
            AddSamplePackage("Foo", "1.0-alpha");
            AddSamplePackage("Bar", "1.0");

            var result = controller.GetMatchingPackages("", false, 30);

            Assert.That(result, Is.EqualTo(new[] { "Bar" }));
        }

        [Test]
        public void GetMatchingPackages_IncludePrerelease()
        {
            AddSamplePackage("Foo", "1.0-alpha");

            var result = controller.GetMatchingPackages("F", true, 30);

            Assert.That(result, Is.EqualTo(new[] { "Foo" }));
        }

        [Test]
        public void GetMatchingPackages_OrderById()
        {
            AddSamplePackage("Zoo", "1.0");
            AddSamplePackage("Foo", "1.0");

            var result = controller.GetMatchingPackages("", false, 30);

            Assert.That(result, Is.EqualTo(new[] { "Foo", "Zoo" }));
        }

        [Test]
        public void GetPackageVersions_NoMatch()
        {
            var result = controller.GetPackageVersions("Foo", false);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetPackageVersions_OrdersByVersion()
        {
            AddSamplePackage("Foo", "2.0");
            AddSamplePackage("Foo", "1.0");

            var result = controller.GetPackageVersions("Foo", false);

            Assert.That(result, Is.EqualTo(new[] { "1.0", "2.0" }));
        }

        [Test]
        public void GetPackageVersions_FiltersById()
        {
            AddSamplePackage("Foo", "2.0");
            AddSamplePackage("Bar", "1.0");

            var result = controller.GetPackageVersions("Foo", false);

            Assert.That(result, Is.EqualTo(new[] { "2.0" }));
        }

        [Test]
        public void GetPackageVersions_FiltersByPrerelease()
        {
            AddSamplePackage("Foo", "2.0");
            AddSamplePackage("Foo", "3.0-alpha");

            var result = controller.GetPackageVersions("Foo", false);

            Assert.That(result, Is.EqualTo(new[] { "2.0" }));
        }

        [Test]
        public void GetPackageVersions_IncludesPrerelease()
        {
            AddSamplePackage("Foo", "1.0");
            AddSamplePackage("Foo", "2.0-pre");

            var result = controller.GetPackageVersions("Foo", true);

            Assert.That(result, Is.EqualTo(new[] { "1.0", "2.0-pre" }));
        }

        private void AddSamplePackage(string id, string version)
        {
            var semanticVersion = new StrictSemanticVersion(version);

            packages.Add(new LucenePackage(_ => new MemoryStream()) { Id = id, Version = semanticVersion });

            var all = packages.Where(p => p.Id == id).OrderBy(p => p.Version).ToList();
            all.ForEach(p => p.IsLatestVersion = p.IsAbsoluteLatestVersion = false);
            var last = all.Last();
            
            last.IsLatestVersion = last.IsAbsoluteLatestVersion = true;
        }
    }
}
