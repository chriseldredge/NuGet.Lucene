using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NuGet.Lucene.Web.Controllers;

namespace NuGet.Lucene.Web.Modules
{
    public class ErrorRoutingModule : HttpModule
    {
        protected override void Init()
        {
            Application.Error += Application_Error;
        }

        /// <summary>
        /// Handles errors that would be handled by system.web/customErrors to
        /// enable routing them to <see cref="ErrorController"/> for logging
        /// and displaying an error message.
        /// </summary>
        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();

            if (!Response.IsClientConnected) return;

            Response.Clear();

            var httpException = exception as HttpException ?? new HttpException(500, "unknown");

            var requestContext = new RequestContext(Context, new RouteData());

            const string controllerName = "Error";
            requestContext.RouteData.Values["controller"] = controllerName;
            requestContext.RouteData.Values["action"] = "HandleError";
            requestContext.RouteData.Values["statusCode"] = httpException.GetHttpCode();

            var controllerFactory = ControllerBuilder.Current.GetControllerFactory();
            var controller = controllerFactory.CreateController(requestContext, controllerName);

            controller.Execute(requestContext);
            Context.Response.End();
        }
    }
}