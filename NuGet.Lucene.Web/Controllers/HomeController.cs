using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NuGet.Lucene.Web.Controllers
{
    public class HomeController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Redirect()
        {
            var location = Url.Link("Status", null);

            if (IsNuGetClient)
            {
                location = Url.Link(RouteNames.PackageFeed, RouteNames.PackageFeedRouteValues);
            }

            var result = Request.CreateResponse(HttpStatusCode.TemporaryRedirect);
            result.Headers.Location = new Uri(location);
            return result;
        }

        private bool IsNuGetClient
        {
            get { return Request.Headers.UserAgent.Any(pi => pi.Product != null && pi.Product.Name == "NuGet"); }
        }
    }
}