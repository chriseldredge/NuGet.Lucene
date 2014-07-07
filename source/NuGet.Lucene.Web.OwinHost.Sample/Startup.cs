using System.IO;
using System.Net;
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
using Microsoft.Owin.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NuGet.Lucene.Web.Filters;
using NuGet.Lucene.Web.Formatters;
using NuGet.Lucene.Web.MessageHandlers;
using NuGet.Lucene.Web.SignalR;
using Owin;

namespace NuGet.Lucene.Web.OwinHost.Sample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            EnvironmentUtility.SetRunningFromCommandLine();
            SignatureConversions.AddConversions(app);
            Start(app, CreateContainer());
        }

        private static void Start(IAppBuilder app, IContainer container)
        {
            var settings = container.Resolve<INuGetWebApiSettings>();

            var config = new HttpConfiguration();
            RegisterServices(container, app, config);
            ConfigureWebApi(config, settings);
            RegisterShutdownCallback(app, container);
            
            if (settings.ShowExceptionDetails)
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
        }

        private static void RegisterShutdownCallback(IAppBuilder app, IContainer container)
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

        private static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<NuGetWebApiModule>();
            builder.RegisterModule<SignalRModule>();

            return builder.Build();
        }

        private static void RegisterServices(IContainer container, IAppBuilder app, HttpConfiguration config)
        {
            var listener = (HttpListener)app.Properties["System.Net.HttpListener"];
            listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication | AuthenticationSchemes.Anonymous;
            
            var apiMapper = container.Resolve<NuGetWebApiRouteMapper>();
            apiMapper.MapApiRoutes(config);
            apiMapper.MapODataRoutes(config);

            var settings = container.Resolve<INuGetWebApiSettings>();
            var signalRMapper = container.Resolve<SignalRMapper>();
            var hubConfiguration = AutofacHubConfiguration.CreateHubConfiguration(container, settings);
            signalRMapper.MapSignalR(app, hubConfiguration);

            var statusHubBroadcaster = new StatusHubUpdateBroadcaster
            {
                ConnectionManager = hubConfiguration.Resolver.Resolve<IConnectionManager>(),
                Repository = container.Resolve<ILucenePackageRepository>()
            };

            statusHubBroadcaster.Start();

            container.CurrentScopeEnding += (s, e) => statusHubBroadcaster.Dispose();

            Swashbuckle.Bootstrapper.Init(config);
        }

        private static void ConfigureWebApi(HttpConfiguration config, INuGetWebApiSettings settings)
        {
            config.IncludeErrorDetailPolicy = settings.ShowExceptionDetails
                ? IncludeErrorDetailPolicy.Always
                : IncludeErrorDetailPolicy.Default;

            config.MessageHandlers.Add(new CrossOriginMessageHandler(settings.EnableCrossDomainRequests));
            config.Filters.Add(new ExceptionLoggingFilter());

            var documentation = new HtmlDocumentation();
            documentation.Load(Directory.GetFiles(".", "*.xml", SearchOption.TopDirectoryOnly));
            config.Services.Replace(typeof(IDocumentationProvider), new WebApiHtmlDocumentationProvider(documentation));
            config.Services.Replace(typeof(IExceptionHandler), new ExceptionHandler());

            var formatter = new NuGetHtmlMicrodataFormatter();
            formatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            formatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            formatter.Settings.Indent = true;
            formatter.Title = "Klondike API";

            config.Formatters.Add(formatter);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.Add(new PackageFormDataMediaFormatter());

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
        }

        private class ExceptionHandler : IExceptionHandler
        {
            public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
            {
                UnhandledExceptionLogger.LogException(context.Exception);
                return Task.FromResult(0);
            }
        }
    }
}