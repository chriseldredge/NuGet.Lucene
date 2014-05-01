using Microsoft.AspNet.SignalR.Hubs;

namespace NuGet.Lucene.Web.SignalR
{
    internal class SignalRLoggingModule : HubPipelineModule
    {
        protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            UnhandledExceptionLogger.LogException(exceptionContext.Error);
            base.OnIncomingError(exceptionContext, invokerContext);
        }
    }
}