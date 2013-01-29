using System.Net;
using System.Web.Mvc;
using Lucene.Net.Linq;
using Ninject;
using NuGet.Lucene.Web.Authentication;

namespace NuGet.Lucene.Web.Controllers
{
    public class UserController : Controller
    {
        [Inject]
        public LuceneDataProvider Provider { get; set; }

        [HttpPost]
        public ActionResult Create(ApiUser user)
        {
            using (var session = Provider.OpenSession<ApiUser>())
            {
                session.Add(user);
            }

            return new HttpStatusCodeResult(HttpStatusCode.Created);
        }
    }
}