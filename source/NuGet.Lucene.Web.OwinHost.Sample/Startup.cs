using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.ExceptionHandling;
using AspNet.WebApi.HtmlMicrodataFormatter;
using Common.Logging;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Ninject;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
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
            Start(app, CreateKernel());
        }

        private static void Start(IAppBuilder app, IKernel kernel)
        {
            var config = new HttpConfiguration();
            RegisterServices(kernel, app, config);
            ConfigureWebApi(config);

            app.UseNinjectMiddleware(() => kernel)
                .UseNinjectWebApi(config);

            RegisterShutdownCallback(app, kernel);
        }

        private static void RegisterShutdownCallback(IAppBuilder app, IKernel kernel)
        {
            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");

            if (token != CancellationToken.None)
            {
                token.Register(kernel.Dispose);
            }
            else
            {
                LogManager.GetCurrentClassLogger().Warn(m => m("host.OnAppDisposing not available."));
            }
        }

        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            kernel.Load<NuGetWebApiModule>();
            kernel.Load<SignalRModule>();
            return kernel;
        }

        private static void RegisterServices(IKernel kernel, IAppBuilder app, HttpConfiguration config)
        {
            var apiMapper = kernel.Get<NuGetWebApiRouteMapper>();
            apiMapper.MapApiRoutes(config);
            apiMapper.MapODataRoutes(config);

            var signalRMapper = kernel.Get<SignalRMapper>();
            signalRMapper.MapSignalR(app);
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