using System;
using System.IO;
using Autofac;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Integration
{
    class IntegrationTestBase
    {
        protected ILucenePackageRepository luceneRepository;

        [SetUp]
        public void SetUp()
        {
            if (WebServerSetUp.SetupException != null)
            {
                throw new Exception("WebServerSetUp failed.", WebServerSetUp.SetupException);
            }

            luceneRepository = WebServerSetUp.AppContainer.Resolve<ILucenePackageRepository>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var p in luceneRepository.LucenePackages)
            {
                luceneRepository.RemovePackage(p);
            }
        }

        protected string ServerUrl
        {
            get { return WebServerSetUp.ServerUrl; }
        }

        protected IPackage LoadSamplePackage(string packageId, string packageVersion)
        {
            var path = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "SamplePackages",
                packageId + "." + packageVersion + ".nupkg");

            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Package file " + path + " not found in Solution packages folder.");
            }

            return new OptimizedZipPackage(path);
        }
    }
}
