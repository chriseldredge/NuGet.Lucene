using Moq;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Tests.Controllers
{
    public abstract class PackagesODataControllerTestBase : ApiControllerTests<PackagesODataController>
    {
        protected Mock<IMirroringPackageRepository> repo;

        protected override PackagesODataController CreateController()
        {
            repo = new Mock<IMirroringPackageRepository>();
            return new PackagesODataController {Repository = repo.Object};
        }
    }
}