using System.Security.Principal;
using Microsoft.Owin;
using Moq;
using NuGet.Lucene.Web.Authentication;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Middleware
{
    public abstract class AuthenticationMiddlewareTestBase
    {
        protected Mock<OwinMiddleware> nextMock;
        protected OwinMiddleware next;
        protected Mock<IOwinContext> contextMock;
        protected IOwinContext context;
        protected Mock<IOwinRequest> requestMock;
        protected IOwinRequest request;
        protected Mock<IOwinResponse> responseMock;
        protected IOwinResponse response;
        protected ApiUserPrincipal user;

        [SetUp]
        public void SetUpContext()
        {
            nextMock = new Mock<OwinMiddleware>((OwinMiddleware)null);
            next = nextMock.Object;

            requestMock = new Mock<IOwinRequest>();
            requestMock.SetupProperty(m => m.User);
            request = requestMock.Object;

            responseMock = new Mock<IOwinResponse>();
            responseMock.SetupProperty(m => m.StatusCode);
            responseMock.SetupProperty(m => m.ReasonPhrase);
            response = responseMock.Object;

            contextMock = new Mock<IOwinContext>();
            contextMock.SetupGet(m => m.Request).Returns(requestMock.Object);
            contextMock.SetupGet(m => m.Response).Returns(responseMock.Object);
            context = contextMock.Object;

            user = new ApiUserPrincipal(new GenericIdentity("T-Rex"), new string[0]);
        }

    }
}