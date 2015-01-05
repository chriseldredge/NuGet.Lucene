using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
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
using NuGet.Lucene.Web.Util;
using Owin;

namespace NuGet.Lucene.Web
{
    public class Startup
    {
        private static readonly ILog Log = LogManager.GetLogger<Startup>();

        protected readonly ManualResetEventSlim shutdownSignal = new ManualResetEventSlim(false);

        protected SignalRMapper signalRMapper;
        public INuGetWebApiSettings Settings { get; set; }

        public void Configuration(IAppBuilder app)
        {
            SetNuGetNotRunningInVisualStudio();
            SignatureConversions.AddConversions(app);
            Settings = CreateSettings();
            Start(app, CreateContainer(app));
        }

        public bool WaitForShutdown(TimeSpan timeout)
        {
            return shutdownSignal.Wait(timeout);
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

            ConfigureWebApi(config, container);

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

            RegisterShutdown(app, container);

            StartIndexingIfConfigured(container);
        }

        protected virtual HttpConfiguration CreateHttpConfiguration()
        {
            return new HttpConfiguration();
        }

        protected virtual void RegisterShutdown(IAppBuilder app, IContainer container)
        {
            var token = app.GetHostAppDisposing();

            if (token.CanBeCanceled)
            {
                token.Register(() => OnShutdown(container));
            }
            else
            {
                Log.Warn(m => m("OWIN property host.OnAppDisposing not available."));
            }
        }

        private async void OnShutdown(IContainer container)
        {
            try
            {
                await ShutdownServices(container);
            }
            finally
            {
                shutdownSignal.Set();
            }
        }

        protected virtual async Task ShutdownServices(IContainer container)
        {
            Log.Info(m => m("Received OnAppDisposing event from OWIN container."));

            var taskRunner = container.Resolve<ITaskRunner>();
            var pendingTasks = taskRunner.PendingTasks;
            if (pendingTasks.Length > 0)
            {
                Log.Info(m => m("Waiting for {0} background tasks.", pendingTasks.Length));
                await Task.WhenAll(pendingTasks);
            }

            Log.Info(m => m("Disposing Autofac application container."));
            container.Dispose();
        }

        protected virtual IContainer CreateContainer(IAppBuilder app)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new OwinAppLifecycleModule(app));
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

        protected virtual void ConfigureWebApi(HttpConfiguration config, IContainer container)
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
            config.Formatters.AddRange(container.Resolve<IEnumerable<MediaTypeFormatter>>());

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

        protected virtual void StartIndexingIfConfigured(IContainer container)
        {
            if (!Settings.SynchronizeOnStart) return;

            var repository = container.Resolve<ILucenePackageRepository>();
            var tcs = container.Resolve<StopSynchronizationCancellationTokenSource>();
            var taskRunner = container.Resolve<ITaskRunner>();

            taskRunner.QueueBackgroundWorkItem(async shutdownCancellationToken =>
            {
                using (shutdownCancellationToken.Register(tcs.Cancel))
                {
                    await repository.SynchronizeWithFileSystem(SynchronizationMode.Incremental, tcs.Token);
                }
            });
        }

        private class LoggingExceptionHandler : IExceptionHandler
        {
            private static readonly Task CompletedTask;

            static LoggingExceptionHandler()
            {
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetResult(true);
                CompletedTask = tcs.Task;
            }

            public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
            {
                UnhandledExceptionLogger.LogException(context.Exception);
                return CompletedTask;
            }
        }
    }
}
