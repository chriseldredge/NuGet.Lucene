using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.ExceptionHandling;
using AspNet.WebApi.HtmlMicrodataFormatter;
using Autofac;
using Autofac.Integration.WebApi;
using Common.Logging;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NuGet.Lucene.Web.Controllers;
using NuGet.Lucene.Web.Filters;
using NuGet.Lucene.Web.Formatters;
using NuGet.Lucene.Web.MessageHandlers;
using Owin;

namespace NuGet.Lucene.Web.OwinHost.Sample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            SignatureConversions.AddConversions(app);

            Start(app, CreateContainer());
        }

        private static void Start(IAppBuilder app, IContainer container)
        {
            var config = new HttpConfiguration();
            RegisterServices(container, app, config);
            ConfigureWebApi(config);
            RegisterShutdownCallback(app, container);

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
                //TODO: does not dispose lucene objects
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
            //container.RegisterModule<SignalRModule>();

            builder.RegisterApiControllers(typeof (IndexingController).Assembly).PropertiesAutowired();

            return builder.Build();
        }

        private static void RegisterServices(IContainer container, IAppBuilder app, HttpConfiguration config)
        {
            var apiMapper = container.Resolve<NuGetWebApiRouteMapper>();
            apiMapper.MapApiRoutes(config);
            apiMapper.MapODataRoutes(config);

            //var signalRMapper = container.Resolve<SignalRMapper>();
            //signalRMapper.MapSignalR(app);
        }

        private static void ConfigureWebApi(HttpConfiguration config)
        {
            config.IncludeErrorDetailPolicy = NuGetWebApiModule.ShowExceptionDetails
                ? IncludeErrorDetailPolicy.Always
                : IncludeErrorDetailPolicy.Default;

            config.MessageHandlers.Add(new CrossOriginMessageHandler(NuGetWebApiModule.EnableCrossDomainRequests));
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