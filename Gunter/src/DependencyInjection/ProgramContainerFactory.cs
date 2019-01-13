using System;
using Autofac;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachements;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;

namespace Gunter.ComponentSetup
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
                throw ExceptionHelper.InitializationException(inner);
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
                throw ExceptionHelper.InitializationException(inner);
            }
        }       
    }
}