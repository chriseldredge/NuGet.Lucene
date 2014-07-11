using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.ExceptionHandling;
using AspNet.WebApi.HtmlMicrodataFormatter;
using Autofac;
using Common.Logging;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Owin;
using Microsoft.Owin.Diagnostics;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NuGet.Lucene.Web.Filters;
using NuGet.Lucene.Web.Formatters;
using NuGet.Lucene.Web.MessageHandlers;
using NuGet.Lucene.Web.SignalR;
using Owin;

namespace NuGet.Lucene.Web
{
    public class Startup
    {
        protected SignalRMapper signalRMapper;
        public INuGetWebApiSettings Settings { get; set; }

        public void Configuration(IAppBuilder app)
        {
            SetNuGetNotRunningInVisualStudio();
            SignatureConversions.AddConversions(app);
            Settings = CreateSettings();
            Start(app, CreateContainer());
        }

        protected virtual INuGetWebApiSettings CreateSettings()
        {
            return new NuGetWebApiSettings();
        }

        protected virtual void SetNuGetNotRunningInVisualStudio()
        {
            // wherein "Command Line" means anything other than Visual Studio:
            EnvironmentUtility.SetRunningFromCommandLine();
        }

        protected virtual void Start(IAppBuilder app, IContainer container)
        {
            var config = CreateHttpConfiguration();

            ConfigureWebApi(config);

            if (Settings.ShowExceptionDetails)
            {
                app.UseErrorPage(new ErrorPageOptions
                {
                    ShowExceptionDetails = true,
                    ShowSourceCode = true
                });
            }
            
            app.UseAutofacMiddleware(container);
            app.UseAutofacWebApi(config);
            app.UseWebApi(config);
            RegisterSignalR(container, app);

            app.UseStageMarker(PipelineStage.MapHandler);

            RegisterServices(container, app, config);
            
            RegisterShutdownCallback(app, container);
        }
    
        protected virtual HttpConfiguration CreateHttpConfiguration()
        {
            return new HttpConfiguration();
        }

        protected virtual void RegisterShutdownCallback(IAppBuilder app, IContainer container)
        {
            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");

            if (token != CancellationToken.None)
            {
                token.Register(container.Dispose);
            }
            else
            {
                LogManager.GetCurrentClassLogger().Warn(m => m("host.OnAppDisposing not available."));
            }
        }

        protected virtual IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new NuGetWebApiModule(Settings));
            builder.RegisterModule<SignalRModule>();

            return builder.Build();
        }

        protected virtual void RegisterServices(IContainer container, IAppBuilder app, HttpConfiguration config)
        {
            var apiMapper = container.Resolve<NuGetWebApiRouteMapper>();

            apiMapper.MapNuGetClientRedirectRoutes(config);
            apiMapper.MapApiRoutes(config);
            apiMapper.MapODataRoutes(config);
            apiMapper.MapSymbolSourceRoutes(config);
        }

        protected virtual void RegisterSignalR(IContainer container, IAppBuilder app)
        {
            signalRMapper = container.Resolve<SignalRMapper>();
            var hubConfiguration = AutofacHubConfiguration.CreateHubConfiguration(container, Settings);
            signalRMapper.MapSignalR(app, hubConfiguration);

            var statusHubBroadcaster = new StatusHubUpdateBroadcaster
            {
                ConnectionManager = hubConfiguration.Resolver.Resolve<IConnectionManager>(),
                Repository = container.Resolve<ILucenePackageRepository>()
            };

            statusHubBroadcaster.Start();
            container.CurrentScopeEnding += (s, e) => statusHubBroadcaster.Dispose();
        }

        protected virtual void ConfigureWebApi(HttpConfiguration config)
        {
            config.IncludeErrorDetailPolicy = Settings.ShowExceptionDetails
                ? IncludeErrorDetailPolicy.Always
                : IncludeErrorDetailPolicy.Default;

            config.MessageHandlers.Add(new CrossOriginMessageHandler(Settings.EnableCrossDomainRequests));
            config.Filters.Add(new ExceptionLoggingFilter());

            var documentation = new HtmlDocumentation();
            documentation.Load();
            config.Services.Replace(typeof(IDocumentationProvider), new WebApiHtmlDocumentationProvider(documentation));
            config.Services.Replace(typeof(IExceptionHandler), new LoggingExceptionHandler());

            var formatter = CreateMicrodataFormatter();

            config.Formatters.Add(formatter);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.Add(new PackageFormDataMediaFormatter());

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
        }

        protected virtual NuGetHtmlMicrodataFormatter CreateMicrodataFormatter()
        {
            var formatter = new NuGetHtmlMicrodataFormatter();
            formatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            formatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            return formatter;
        }

        private class LoggingExceptionHandler : IExceptionHandler
        {
            public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
            {
                return Task.Factory.StartNew(() => UnhandledExceptionLogger.LogException(context.Exception), cancellationToken);
            }
        }
    }
}