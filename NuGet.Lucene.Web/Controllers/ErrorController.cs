using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Common.Logging;

namespace NuGet.Lucene.Web.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult HandleError(int statusCode)
        {
            var ex = Server.GetLastError() ?? new HttpException(statusCode, "");
            LogLastError(ex);
            Server.ClearError();
            
            Response.TrySkipIisCustomErrors = true;
            Response.StatusCode = statusCode;

            return View("error", ex as HttpException ?? new HttpException(statusCode, "", ex));
        }

        public ActionResult Throw(int statusCode)
        {
            throw new HttpException(statusCode, "Thrown for testing purposes.");
        }

        private void LogLastError(Exception ex)
        {
            var httpError = ex as HttpException;
            string message;
            if (httpError != null)
            {
                message = String.Format("HTTP {0} {1}: on URI {2}: {3}", httpError.GetHttpCode(), (HttpStatusCode)httpError.GetHttpCode(), OriginatingUri, ex.Message);
            }
            else
            {
                message = String.Format("Unhandled Exception: on URI: {0}: {1}", OriginatingUri, ex.Message);
            }

            var log = GetLogSeverityDelegate(httpError);

            log(m => m(message), ex.StackTrace != null ? ex : null);
        }

        /// <summary>
        /// When system.webServer/httpErrors routes an error here, <see cref="HttpRequestBase.Url"/>
        /// will look like <c>http://example.com/error/404?404;/xyz</c>. This property
        /// simplifies this back to e.g. <c>http://example.com/xyz</c>.
        /// </summary>
        private Uri OriginatingUri
        {
            get
            {
                var queryString = Request.RawUrl.Split(new[] {'?'}, 1, StringSplitOptions.None).Last();
                var pathAndQuery = queryString.Split(';').Last();
                try
                {
                    return new Uri(Request.Url, pathAndQuery);
                }
                catch (UriFormatException)
                {
                    return Request.Url;
                }
                
            }
        }

        private Action<Action<FormatMessageHandler>, Exception> GetLogSeverityDelegate(Exception exception)
        {
            if (exception is HttpRequestValidationException || exception is ViewStateException)
            {
                return UnhandledExceptionLogger.Log.Warn;
            }

            var httpError = exception as HttpException;
            if (httpError != null && (httpError.ErrorCode == unchecked((int)0x80070057) || httpError.ErrorCode == unchecked((int)0x800704CD)))
            {
                // "The remote host closed the connection."
                return UnhandledExceptionLogger.Log.Debug;
            }

            if (httpError != null && (httpError.GetHttpCode() < 500))
            {
                return UnhandledExceptionLogger.Log.Info;
            }

            return UnhandledExceptionLogger.Log.Error;
        }
    }
}