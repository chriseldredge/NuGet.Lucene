using System;
using Moq;
using NuGet.Lucene.Web.Formatters;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Formatters
{
    [TestFixture]
    public class PackageFormDataMediaFormatterTests
    {
        [Test]
        [TestCase(typeof(IPackage))]
        [TestCase(typeof(ZipPackage))]
        public void SupportsTypes(Type type)
        {
            new PackageFormDataMediaFormatter(new Mock<ILucenePackageRepository>().Object).CanReadType(type);
        }
    }
}
