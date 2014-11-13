using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Util;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    [TestFixture]
    public class IndexingControllerTests : ApiControllerTests<IndexingController>
    {
        private Mock<ILucenePackageRepository> repository;
        private Mock<ITaskRunner> taskRunner;

        [SetUp]
        public void SetUp()
        {
            SetUpRequest(RouteNames.Indexing, HttpMethod.Get, "api/indexing/status");
        }

        protected override IndexingController CreateController()
        {
            repository = new Mock<ILucenePackageRepository>();
            taskRunner = new Mock<ITaskRunner>();

            return new IndexingController
                {
                    Repository = repository.Object,
                    UserRequestedCancellationTokenSource = new StopSynchronizationCancellationTokenSource(),
                    TaskRunner = taskRunner.Object
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
            var token = controller.UserRequestedCancellationTokenSource.Token;

            controller.Cancel();

            Assert.That(token.IsCancellationRequested, Is.True, "IsCancellationRequested");
        }

        [Test]
        public void Synchronize()
        {
            repository.Setup(r => r.GetStatus()).Returns(CreateSampleStatus());
            taskRunner.Setup(t => t.QueueBackgroundWorkItem(It.IsAny<Func<CancellationToken, Task>>()))
                .Callback<Func<CancellationToken, Task>>(f => f(CancellationToken.None));

            var result = controller.Synchronize();

            repository.Verify(r => r.SynchronizeWithFileSystem(controller.UserRequestedCancellationTokenSource.Token), Times.Once());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        }

        [Test]
        public void SynchronizeAlreadyRunning()
        {
            repository.Setup(r => r.GetStatus()).Returns(CreateSampleStatus(SynchronizationState.ScanningFiles));

            var result = controller.Synchronize();

            repository.Verify(r => r.SynchronizeWithFileSystem(controller.UserRequestedCancellationTokenSource.Token), Times.Never());

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        private RepositoryInfo CreateSampleStatus(SynchronizationState state = SynchronizationState.Idle)
        {
            return new RepositoryInfo(0, new IndexingStatus(IndexingState.Idle, new SynchronizationStatus(state)));
        }
    }
}
