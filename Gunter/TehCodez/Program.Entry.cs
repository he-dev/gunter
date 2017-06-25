using System;
using System.Collections.Generic;
using System.Linq;
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
    internal partial class Program
    {
        internal static int Main(string[] args)
        {
            return Start(
                args,
                InitializeLogging,
                InitializeConfiguration,
                InitializeContainer);
        }

        public static int Start(
            string[] args,
            Action initializeLogging,
            Func<Configuration> initializeConfiguration,
            Func<Configuration, IContainer> initializeContainer)
        {
            initializeLogging();

            var mainLogger = LoggerFactory.CreateLogger(nameof(Program)); LogEntry.New().Debug().Message("Logging initialized.").Log(mainLogger);
            var mainLogEntry = LogEntry.New().Stopwatch(sw => sw.Start());

            try
            {
                var configuration = initializeConfiguration(); LogEntry.New().Debug().Message("Configuration initialized.").Log(mainLogger);
                var container = initializeContainer(configuration); LogEntry.New().Debug().Message("IoC initialized.").Log(mainLogger);

                using (var scope = container.BeginLifetimeScope())
                {
                    var program = scope.Resolve<Program>();
                    LogEntry.New().Info().Message($"Created {Name} v{Version}").Log(mainLogger);
                    program.Start(args);
                }

                mainLogEntry.Info().Message("Completed.");
                return 0;
            }
            catch (Exception ex)
            {
                mainLogEntry.Fatal().Message("Crashed.").Exception(ex);
                return 1;
            }
            finally
            {
                mainLogEntry.Log(mainLogger);
                LogEntry.New().Info().Message("Exited.").Log(mainLogger);
            }
        }

        #region Initialization

        internal static void InitializeLogging()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
            
            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting(name: "Environment", key: $"Gunter.Program.Config.Environment"));
            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
        }

        internal static Configuration InitializeConfiguration()
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

        internal static IContainer InitializeContainer(Configuration configuration)
        {
            return InitializeContainer(configuration, null);
        }

        internal static IContainer InitializeContainer(Configuration configuration, Autofac.Module overrideModule)
        {
            try
            {
                var builder = new ContainerBuilder();

                builder.RegisterInstance(configuration.Load<Program, Workspace>());

                builder.RegisterModule<SystemModule>();
                builder.RegisterModule<DataModule>();
                builder.RegisterModule<ReportingModule>();
                builder.RegisterModule<HtmlModule>();

                builder
                    .RegisterType<TestRunner>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

                builder
                    .RegisterType<Program>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Program))))
                    .PropertiesAutowired();

                if (overrideModule != null) { builder.RegisterModule(overrideModule); }

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
