using System;
using NUnit.Framework;
using NuGet.Lucene.Web.Formatters;

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
            new PackageFormDataMediaFormatter().CanReadType(type);
        }
    }
}