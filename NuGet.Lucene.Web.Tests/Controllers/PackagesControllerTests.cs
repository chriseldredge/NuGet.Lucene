using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Mvc;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class PackagesControllerTests
    {
        private PackagesController controller;
        private Mock<ILucenePackageRepository> repository;
        private List<LucenePackage> packages;

        [SetUp]
        public void SetUp()
        {
            packages = new List<LucenePackage>();
            repository = new Mock<ILucenePackageRepository>();
            controller = new PackagesController { Repository = repository.Object };

            repository.Setup(repo => repo.LucenePackages).Returns(packages.AsQueryable());
        }

        [Test]
        [TestCase("Upload", "PUT", "POST")]
        [TestCase("Delete", "DELETE")]
        public void ActionsRequireAuthentication(string action, params string[] verbs)
        {
            AssertAuthenticationAttributePresent(action, verbs);
        }

        private void AssertAuthenticationAttributePresent(string action, IEnumerable<string> verbs)
        {
            var method = controller.GetType().GetMethod(action);

            Assert.That(method, Is.Not.Null, "Action method " + action + " not found on controller type " + controller.GetType());
            var acceptVerbsAttribute = method.GetCustomAttribute<AcceptVerbsAttribute>();

            Assert.That(acceptVerbsAttribute, Is.Not.Null, "AcceptVerbsAttribute should decorate " + action + " action.");

            Assert.That(acceptVerbsAttribute.Verbs, Is.EquivalentTo(verbs));
        }
    }
}