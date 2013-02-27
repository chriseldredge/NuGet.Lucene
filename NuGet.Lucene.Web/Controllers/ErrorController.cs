using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

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
                message = string.Format("HTTP {0} {1}: on URI {2}: {3}", httpError.GetHttpCode(), (HttpStatusCode)httpError.GetHttpCode(), OriginatingUri, ex.Message);
            }
            else
            {
                message = string.Format("Unhandled Exception: on URI: {0}: {1}", OriginatingUri, ex.Message);
            }

            UnhandledExceptionLogger.LogException(httpError, message);
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
    }
}