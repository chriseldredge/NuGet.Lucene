using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Microsoft.Owin;

namespace NuGet.Lucene.Web.Filters
{
    public class ExceptionLoggingFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            UnhandledExceptionLogger.Log.Error(actionExecutedContext.Exception.Message, actionExecutedContext.Exception);
        }
    }
}
