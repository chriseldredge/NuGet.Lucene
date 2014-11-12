using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using Autofac;
using Lucene.Net.Linq;
using Lucene.Net.Store;
using NuGet.Lucene.Web.OwinHost.Sample;
using NuGet.Lucene.Web.SignalR;
using NUnit.Framework;
using Owin;
using Directory = System.IO.Directory;
using Version = Lucene.Net.Util.Version;

namespace NuGet.Lucene.Web.Tests.Integration
{
    [SetUpFixture]
    internal class WebServerSetUp
    {
        internal static readonly string LocalAdministratorApiKey = Guid.NewGuid().ToString();

        private static TestableProgram program;

        internal static int Port { get; set; }

        internal static string ServerUrl
        {
            get { return string.Format("http://{0}:{1}/", IPAddress.Loopback, Port); }
        }

        internal static Exception SetupException { get; private set; }

        protected NameValueCollection Settings { get; private set; }

        public WebServerSetUp()
        {
            Settings = new NameValueCollection();
            Settings["packageMirrorTargetUrl"] = "https://www.nuget.org/api/v2/";
            Settings["localAdministratorApiKey"] = LocalAdministratorApiKey;
        }

        [SetUp]
        public void StartWebServer()
        {
            if (program != null)
            {
                return;
            }

            try
            {
                Port = GetRandomUnusedPort();
                program = new TestableProgram(Settings);
                program.Start(ServerUrl);
            }
            catch (Exception ex)
            {
                // nCrunch does not report exception details, so we save it and rethrow from
                // IntegrationTestBase.SetUp().
                SetupException = ex;
            }
        }

        [TearDown]
        public void ShutdownWebServer()
        {
            program.Dispose();
            program = null;
            if (Directory.Exists("App_Data"))
            {
                Directory.Delete("App_Data", recursive:true);
            }
        }

        internal static IContainer AppContainer
        {
            get
            {
                return program.Container;
            }
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint) listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        class TestableProgram : Program
        {
            private readonly NameValueCollection settings;

            public TestableProgram(NameValueCollection settings)
            {
                this.settings = settings;
            }

            public IContainer Container { get; private set; }
            
            protected override IContainer CreateContainer(IAppBuilder app)
            {
                var builder = new ContainerBuilder();
                builder.RegisterModule(new OwinAppLifecycleModule(app));
                builder.RegisterModule(new InMemoryLuceneNuGetWebApiModule(
                    new NuGetWebApiSettings(
                        NuGetWebApiSettings.BlankAppSettingPrefix,
                        settings,
                        new NameValueCollection())));

                builder.RegisterModule<SignalRModule>();
                Container = builder.Build();
                return Container;
            }
        }

        class InMemoryLuceneNuGetWebApiModule : NuGetWebApiModule
        {
            public InMemoryLuceneNuGetWebApiModule(INuGetWebApiSettings settings) : base(settings)
            {
            }

            protected override ILuceneRepositoryConfigurator InitializeRepositoryConfigurator(INuGetWebApiSettings settings)
            {
                var cfg = new InMemoryLuceneRepositoryConfigurator
                {
                    EnablePackageFileWatcher = false,
                    GroupPackageFilesById = false,
                    PackagePath = settings.PackagesPath,
                    PackageOverwriteMode = settings.PackageOverwriteMode
                };

                cfg.Initialize();

                return cfg;
            }

            protected override LuceneDataProvider InitializeUsersDataProvider(INuGetWebApiSettings settings)
            {
                return new LuceneDataProvider(new RAMDirectory(), Version.LUCENE_30);
            }
        }

        class InMemoryLuceneRepositoryConfigurator : LuceneRepositoryConfigurator
        {
            protected override void CreateDirectories()
            {
                if (!Directory.Exists(PackagePath))
                {
                    Directory.CreateDirectory(PackagePath);
                }
            }

            protected override global::Lucene.Net.Store.Directory OpenLuceneDirectory(string luceneIndexPath)
            {
                return new RAMDirectory();
            }
        }
    }
}
