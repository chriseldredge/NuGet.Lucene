using System;
using System.Net;
using System.Net.Http;
using Moq;
using NUnit.Framework;
using NuGet.Lucene.Web.Controllers;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class IndexingControllerTests : ApiControllerTests<IndexingController>
    {
        private Mock<ILucenePackageRepository> repository;

        [SetUp]
        public void SetUp()
        {
            SetUpRequest(RouteNames.PackageApi, HttpMethod.Put, "api/v2/package");
        }

        protected override IndexingController CreateController()
        {
            repository = new Mock<ILucenePackageRepository>();

            return new IndexingController
                {
                    Repository = repository.Object,
                    CancellationTokenSource = new ReusableCancellationTokenSource(),
                    FireAndForget = (func, error) => func()
                };
        }

        [Test]
        public void GetStatus()
        {
            var status = CreateSampleStatus();

            repository.Setup(r => r.GetStatus()).Returns(status);

            Assert.That(controller.Status(), Is.SameAs(status));
        }

        [Test]
        public void Cancel()
        {
            var token = controller.CancellationTokenSource.Token;

            controller.Cancel();

            Assert.That(token.IsCancellationRequested, Is.True, "IsCancellationRequested");
        }

        [Test]
        public void Synchronize()
        {
            repository.Setup(r => r.GetStatus()).Returns(CreateSampleStatus());

            var result = controller.Synchronize();

            repository.Verify(r => r.SynchronizeWithFileSystem(controller.CancellationTokenSource.Token), Times.Once());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void SynchronizeAlreadyRunning()
        {
            repository.Setup(r => r.GetStatus()).Returns(CreateSampleStatus(SynchronizationState.ScanningFiles));

            var result = controller.Synchronize();

            repository.Verify(r => r.SynchronizeWithFileSystem(controller.CancellationTokenSource.Token), Times.Never());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        private RepositoryInfo CreateSampleStatus(SynchronizationState state = SynchronizationState.Idle)
        {
            return new RepositoryInfo(0, new IndexingStatus(IndexingState.Idle, new SynchronizationStatus(state)));
        }
    }
}