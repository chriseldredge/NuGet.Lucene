using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using Common.Logging;

namespace NuGet.Lucene.Web
{
    public static class UnhandledExceptionLogger
    {
        internal static readonly ILog Log = LogManager.GetLogger(typeof(UnhandledExceptionLogger));

        public static void LogException(Exception exception)
        {
            LogException(exception, m => m("Unhandled exception: {0}: {1}", exception.GetType(), exception.Message));
        }

        public static void LogException(Exception exception, Action<FormatMessageHandler> formatMessageCallback)
        {
            var log = GetLogSeverityDelegate(exception);

            log(formatMessageCallback, exception.StackTrace != null ? exception : null);
        }

        private static Action<Action<FormatMessageHandler>, Exception> GetLogSeverityDelegate(Exception exception)
        {
            if (exception is HttpRequestValidationException || exception is ViewStateException)
            {
                return Log.Warn;
            }

            if (exception is TaskCanceledException || exception is OperationCanceledException)
            {
                return Log.Info;
            }

            var httpError = exception as HttpException;
            if (httpError != null && (httpError.ErrorCode == unchecked((int)0x80070057) || httpError.ErrorCode == unchecked((int)0x800704CD)))
            {
                // "The remote host closed the connection."
                return Log.Debug;
            }

            if (httpError != null && (httpError.GetHttpCode() < 500))
            {
                return Log.Info;
            }

            return Log.Error;
        }
    }
}
