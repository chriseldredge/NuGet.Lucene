using System.Threading.Tasks;
using Microsoft.Owin;
using Moq;
using NuGet.Lucene.Web.Middleware;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Middleware
{
    [TestFixture]
    public class AuthenticationMiddlewareBaseTests : AuthenticationMiddlewareTestBase
    {
        protected TestableAuthenticationMiddlewareBase middleware;
        protected Mock<AuthenticationHandlerBase> handlerMock;

        [SetUp]
        public void SetUp()
        {
            handlerMock = new Mock<AuthenticationHandlerBase>();
            middleware = new TestableAuthenticationMiddlewareBase(nextMock.Object, handlerMock.Object);
        }

        [Test]
        public async Task HandlerReturnsTrue_DelegatesToNext()
        {
            handlerMock.Setup(m => m.InvokeAsync()).Returns(Task.FromResult(true));

            await middleware.Invoke(context);

            nextMock.Verify(m => m.Invoke(context), Times.Once);
        }

        [Test]
        public async Task InitializeAndInvokeHandler()
        {
            await middleware.Invoke(context);

            handlerMock.Verify(m => m.InitializeAsync(context), Times.Once);
            handlerMock.Verify(m => m.InvokeAsync(), Times.Once);
        }

        [Test]
        public async Task HandlerReturnsFalse_SkipsNext()
        {
            handlerMock.Setup(m => m.InvokeAsync()).Returns(Task.FromResult(false));

            await middleware.Invoke(context);

            nextMock.Verify(m => m.Invoke(context), Times.Never);
        }

        public class TestableAuthenticationMiddlewareBase : AuthenticationMiddlewareBase
        {
            private readonly AuthenticationHandlerBase handler;

            public TestableAuthenticationMiddlewareBase(OwinMiddleware next, AuthenticationHandlerBase handler) : base(next)
            {
                this.handler = handler;
            }

            protected override AuthenticationHandlerBase CreateHandler()
            {
                return handler;
            }
        }
    }
}
