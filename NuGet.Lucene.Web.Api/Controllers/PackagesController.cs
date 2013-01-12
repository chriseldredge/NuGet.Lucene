using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData.Query;
using Ninject;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Controllers
{
    public class PackagesController : ApiController
    {
        [Inject]
        public ILucenePackageRepository Repository { get; set; }

        [Queryable(HandleNullPropagation = HandleNullPropagationOption.False)]
        public IQueryable<ApiV2Package> Get()
        {
            return Repository.LucenePackages.Select(p => new ApiV2Package(p));
        }
    }
}