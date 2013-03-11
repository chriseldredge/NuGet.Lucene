using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Hosting;
using System.Web.Http;

namespace NuGet.Lucene.Web.Controllers
{
    public class HomeController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage DefaultDocument()
        {
            var message = Request.CreateResponse(HttpStatusCode.OK);
            message.Content = new StringContent(GetContents(), Encoding.UTF8);
            message.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("text/html");
            return message;
        }

        private static string contents;
        private static DateTime? lastWriteTime;

        private string GetContents()
        {
            var path = HostingEnvironment.MapPath("~/App.html");
            var writeTime = File.GetLastWriteTime(path);

            if (lastWriteTime == null || lastWriteTime != writeTime)
            {
                contents = File.ReadAllText(path);
                lastWriteTime = writeTime;
            }

            return contents;
        }
    }
}