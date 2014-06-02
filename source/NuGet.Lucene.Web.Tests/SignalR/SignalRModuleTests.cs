using Autofac;
using Moq;
using NuGet.Lucene.Web.SignalR;
using NuGet.Lucene.Web.SignalR.Hubs;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.SignalR
{
    [TestFixture]
    public class SignalRModuleTests
    {
        private SignalRModule module;
        private IContainer container;
        private Mock<ILucenePackageRepository> repository;

        [Test]
        public void RegistersMapper()
        {
            BuildContainer();

            TestDelegate call = () => container.Resolve<SignalRMapper>();

            Assert.That(call, Throws.Nothing);
        }

        [Test]
        public void RegistersPerRequestStatusHub()
        {
            BuildContainer();

            var h1 = container.Resolve<StatusHub>();
            var h2 = container.Resolve<StatusHub>();

            Assert.That(h1, Is.Not.SameAs(h2));
        }

        [Test]
        public void StatusHubHasDependencies()
        {
            BuildContainer();

            var hub = container.Resolve<StatusHub>();

            Assert.That(hub.Repository, Is.SameAs(repository.Object));
        }

        private void BuildContainer()
        {
            var builder = new ContainerBuilder();
            
            repository = new Mock<ILucenePackageRepository>();
            builder.RegisterInstance(repository.Object);

            module = new SignalRModule();
            builder.RegisterModule(module);
            container = builder.Build();
        }

    }
}
