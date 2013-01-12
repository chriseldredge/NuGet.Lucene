using System;
using System.Web;
using System.Web.Http;
using NuGet.Lucene.Web.Mvc4.Api;

namespace NuGet.Lucene.Web
{
    public class Global : HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            WebApiConfig.Register(GlobalConfiguration.Configuration);
        }
        
    }
}