using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autofac;
using Gunter.Data.Configurations;
using Gunter.Alerting;
using Gunter.Alerting.Email;
using Gunter.Testing;
using Newtonsoft.Json;
using SmartConfig.DataStores.AppConfig;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Reflection;
using Gunter.Services;

namespace Gunter
{
    internal class Program
    {
#if DEBUG
        public const string InstanceName = "Gunter.debug";
#else
        public const string InstanceName = "Gunter";
#endif        

        private static ILogger _logger;

        private static int Main(string[] args)
        {
            try
            {
                InitializeLogger();

                LogEntry.New().Message($"*** {InstanceName} v1.0.0 ***").Log(_logger);

                InitializeConfiguration();
                var container = InitializeContainer();

                var constants = ConfigurationReader.ReadGlobals();
                var testConfigs = ConfigurationReader.ReadTests(container).ToArray();

                using (var scope = container.BeginLifetimeScope())
                {
                    scope.Resolve<TestRunner>().RunTests(testConfigs, constants);
                }

                return 0;
            }
            // Exception should already be logged elsewhere and rethrown to exit the application.
            catch (Exception ex)
            {
                LogEntry.New().Fatal().Message("Crashed.").Exception(ex).Log(_logger);
                return 1;
            }
        }

        private static void InitializeLogger()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
            Reusable.Logging.NLog.Tools.DatabaseTargetQueryGenerator.GenerateInsertQueries();

            Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting("Environment", "Environment"));
            Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
            _logger = LoggerFactory.CreateLogger("Program");

        }

        private static IContainer InitializeContainer()
        {
            var containerBuilder = new ContainerBuilder();

            // Configure dependencies.

            //containerBuilder
            //    .RegisterType<EmailAlert>()
            //    .As<IAlert>();

            //containerBuilder.RegisterInstance(new StateRepository(AppSettingsConfig.Environment));

            //containerBuilder
            //    .RegisterInstance(constants)
            //    .As<IConstantResolver>();

            containerBuilder
                .RegisterType<Data.SqlClient.TableOrViewDataSource>()
                .As<IDataSource>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Data.SqlClient.TableOrViewDataSource))));

            //containerBuilder
            //    .RegisterType<EmailService>()
            //    .As<IEmailService>()
            //    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.GetLogger<EmailService>()));

            containerBuilder
                .RegisterType<EmailAlert>()
                .As<IAlert>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(EmailAlert))));

            containerBuilder
                .RegisterType<TestRunner>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

            return containerBuilder.Build();
        }

        private static void InitializeConfiguration()
        {
            using (var entry = LogEntry.New().Stopwatch(sw => sw.Start()).AsAutoLog(_logger))
            {
                try
                {
                    //SmartConfig.Configuration.Load
                    //       .From(new ConnectionStringsStore())
                    //       .Select(typeof(ConnectionStringsConfig));

                    SmartConfig.Configuration.Builder()
                           .From(new AppSettingsStore())
                           .Select(typeof(AppSettingsConfig));

                    //SmartConfig.Configuration.Load
                    //    .From(new SQLiteStore("name=ConfigDb"))
                    //    .Where("Environment", AppSettingsConfig.Environment)
                    //    .Register<JsonToObjectConverter<LogInfo>>()
                    //    .Select(typeof(MainConfig));

                    entry.Message("Loaded configuration in {ElapsedSeconds} seconds.");

                }
                catch (Exception ex)
                {
                    entry.Error().Exception(ex).Message("Could not load configuration in {ElapsedSeconds} seconds.");
                    throw;
                }
            }
        }
       
        //private static int Exit(ExitCode errorCode)
        //{
        //    _logger.Info().MessageFormat("*** Exited with error code {ErrorCode}.", new { ErrorCode = (int)errorCode }).Log();
        //    _logger.Info().Message("*** Good bye!.").Log();
        //    return (int)errorCode;
        //}
    }

    internal class FileName
    {
        public const string Globals = "Globals";
        public const string Templates = "_Templates";
    }


}
