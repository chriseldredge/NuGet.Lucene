using System;
using System.Web.Http.OData;
using System.Web.Http.Routing;
using NuGet.Lucene.Web.Models;
using NuGet.Lucene.Web.OData.Formatter.Serialization;
using NuGet.Lucene.Web.Tests.Controllers;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.OData.Formatter.Serialization
{
    [TestFixture]
    public class ODataPackageDefaultStreamAwareEntityTypeSerializerTests : PackagesODataControllerTestBase
    {
        ODataPackageDefaultStreamAwareEntityTypeSerializer serializer;
        EntityInstanceContext context;

        [SetUp]
        public void SetUp()
        {
            SetUpRequestWithOptions("/api/odata");

            serializer = new ODataPackageDefaultStreamAwareEntityTypeSerializer(
                new ODataPackageDefaultStreamAwareSerializerProvider());

            context = new EntityInstanceContext
            {
                EdmModel = model,
                Request = request
            };
        }

        [Test]
        public void BuildsAbsoluteUriToPackageDownload()
        {
            var result = serializer.BuildLinkForStreamProperty(new ODataPackage { Id = "Sample", Version = "1.0" }, context);

            Assert.That(result.IsAbsoluteUri, Is.True, "IsAbsoluteUri");

            var expected = new UrlHelper(request).Link(RouteNames.Packages.Download, new { Id = "Sample", Version = "1.0" });
            Assert.That(result.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped), Is.EqualTo(expected));
        }
    }
}

