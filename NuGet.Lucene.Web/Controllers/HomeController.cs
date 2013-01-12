using System.Web.Mvc;

namespace NuGet.Lucene.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Redirect()
        {
            return RedirectToRoute(Global.PackageFeedRouteName, Global.PackageFeedRouteValues);
        }
    }
}