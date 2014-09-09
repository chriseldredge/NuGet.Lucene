using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;
using Moq;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    public abstract class PackagesODataControllerTestBase : ApiControllerTests<PackagesODataController>
    {
        protected Mock<IMirroringPackageRepository> repo;
        protected IEdmModel model;

        protected override PackagesODataController CreateController()
        {
            repo = new Mock<IMirroringPackageRepository>();

            var builder = new NuGetWebApiODataModelBuilder();
            builder.Build();

            model = builder.Model;

            return new PackagesODataController {Repository = repo.Object};
        }

        protected ODataQueryOptions<ODataPackage> SetUpRequestWithOptions(string path)
        {
            SetUpRequest(RouteNames.Packages.Feed, HttpMethod.Post, path);
            return new ODataQueryOptions<ODataPackage>(new ODataQueryContext(model, typeof(ODataPackage)), request);
        }
    }
}
