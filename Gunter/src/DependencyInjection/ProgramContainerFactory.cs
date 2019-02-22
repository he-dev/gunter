using System;
using Autofac;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionizer;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachments;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.DependencyInjection
{
    public static class ProgramContainerFactory
    {
        public static IContainer CreateContainer() => CreateContainer(InitializeLogging(), _ => { });

        public static IContainer CreateContainer(ILoggerFactory loggerFactory, Action<ContainerBuilder> configureContainer)
        {
            try
            {
                var builder = new ContainerBuilder();

                builder
                    .RegisterInstance(loggerFactory)
                    .ExternallyOwned()
                    .As<ILoggerFactory>();

                builder
                    .RegisterGeneric(typeof(Logger<>))
                    .As(typeof(ILogger<>));

                builder
                    .RegisterType<ProgramInfo>()
                    .AsSelf();

                builder.RegisterModule<Service>();
                builder.RegisterModule<Data>();
                builder.RegisterModule<Reporting>();
                builder.RegisterModule<Mailr>();

                configureContainer?.Invoke(builder);

                return builder.Build();
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("ContainerInitialization", "Could not initialize program container.", inner);
            }
        }

        [NotNull]
        internal static ILoggerFactory InitializeLogging()
        {
            try
            {
                Reusable.Utilities.NLog.LayoutRenderers.SmartPropertiesLayoutRenderer.Register();

                var loggerFactory =
                    new LoggerFactory()
                        .AttachObject("Environment", System.Configuration.ConfigurationManager.AppSettings["app:Environment"])
                        .AttachObject("Product", ProgramInfo.FullName)
                        .AttachScope()
                        .AttachSnapshot()
                        .Attach<Timestamp<DateTimeUtc>>()
                        .AttachElapsedMilliseconds()
                        .AddObserver<NLogRx>();

                return loggerFactory;
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("LoggerInitialization", "Could not initialize logger.", inner);
            }
        }
    }
}