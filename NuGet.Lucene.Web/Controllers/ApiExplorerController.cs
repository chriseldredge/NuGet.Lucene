using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Controllers
{
    public class ApiExplorerController : ApiController
    {
        public IEnumerable<SimpleApiDescription> GetApiMethods()
        {
            return WebApiDescriptions
                .Select(i => new SimpleApiDescription(Request, i))
                .Union(OtherDescriptions);
        }

        protected IEnumerable<SimpleApiDescription> OtherDescriptions
        {
            get
            {
                yield return new SimpleApiDescription(Request, "OData", "api/odata");
            }
        }

        protected IEnumerable<ApiDescription> WebApiDescriptions
        {
            get
            {
                var apiExplorer = Configuration.Services.GetApiExplorer();

                return apiExplorer.ApiDescriptions;
            }
        }
    }
}
