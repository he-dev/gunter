using System;
using Autofac;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Threading.Tasks;
using Autofac.Extras.AggregateService;
using Gunter.AutofacModules;
using Gunter.Messaging.Email;
using Gunter.Messaging.Email.ModuleRenderers;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Gunter.Services;
using JetBrains.Annotations;
using NLog.Fluent;
using Reusable.ConfigWhiz;
using Reusable.ConfigWhiz.Datastores.AppConfig;
using Reusable.Markup.Html;
using Module = Gunter.Reporting.Module;

namespace Gunter
{
    internal class Program
    {
        private static readonly ILogger Logger;

        static Program()
        {
            Logger = InitializeLogging();
            Configuration = InitializeConfiguration();
        }

        public static Configuration Configuration { get; }

        private static int Main(string[] args)
        {
            var mainLogEntry =
                LogEntry
                    .New()
                    .MessageBuilder(sb => sb.Append($"*** {TehApplicashun.Name} v{TehApplicashun.Version}"))
                    .Stopwatch(sw => sw.Start());

            try
            {
                var container = InitializeContainer();
                using (var scope = container.BeginLifetimeScope())
                {
                    var tehApp = scope.Resolve<TehApplicashun>();
                    tehApp.Start(args);
                }

                mainLogEntry.Info().MessageBuilder(sb => sb.Append("completed."));
                return 0;
            }
            catch (Exception ex)
            {
                mainLogEntry.Fatal().MessageBuilder(sb => sb.Append("crashed.")).Exception(ex);
                return 1;
            }
            finally
            {
                mainLogEntry.MessageBuilder(sb => sb.Append(" ***")).Log(Logger);
            }
        }

        #region Initialization

        private static ILogger InitializeLogging()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();

            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting(name: "Environment", key: $"Gunter.Program.Config.Environment"));
            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
            var logger = LoggerFactory.CreateLogger(nameof(Program));
            LogEntry.New().Debug().Message("Logging initialized.").Log(logger);
            return logger;
        }

        private static Configuration InitializeConfiguration()
        {
            try
            {
                return new Configuration(new AppSettings());
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize configuration.", ex);
            }
        }

        private static IContainer InitializeContainer()
        {
            try
            {
                var builder = new ContainerBuilder();

                builder
                    .RegisterInstance(Configuration.Load<TehApplicashun, Workspace>());

                builder
                    .RegisterModule<SystemModule>();

                builder
                    .RegisterModule<DataModule>();

                builder
                    .RegisterModule<ReportingModule>();

                builder
                    .RegisterModule<HtmlModule>();

                builder
                    .RegisterType<TestRunner>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

                builder
                    .RegisterType<TehApplicashun>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TehApplicashun))))
                    .PropertiesAutowired();

                LogEntry.New().Debug().Message("IoC initialized.").Log(Logger);

                return builder.Build();
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize container.", ex);
            }
        }

        #endregion
    }

    internal class InitializationException : Exception
    {
        public InitializationException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
