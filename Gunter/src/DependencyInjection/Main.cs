using System;
using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Reusable.OmniLog;
using Reusable.SmartConfig;
using AutofacModule = Autofac.Module;

namespace Gunter.DependencyInjection
{
    internal class Main : AutofacModule
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;

        private Main(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _loggerFactory = loggerFactory;
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {

            builder
                .RegisterInstance(_loggerFactory)
                .ExternallyOwned()
                .As<ILoggerFactory>();

            builder
                .RegisterGeneric(typeof(Logger<>))
                //.Named("logger", typeof(ILogger<>))
                .As(typeof(ILogger<>));

            builder
                .RegisterInstance(_configuration)
                .As<IConfiguration>();

            builder
                .RegisterType<Program>()
                .AsSelf();

            builder
                .RegisterType<TestBundle>()
                .AsSelf();

            builder
                .RegisterType<TestFileProvider>()
                .As<ITestFileProvider>();

            builder
                .RegisterType<TestFileSerializer>()
                .AsSelf();

            builder
                .RegisterInstance(RuntimeVariableHelper.EnumerateVariables());
        }

        public static IContainer Create(ILoggerFactory loggerFactory, IConfiguration configuration, AutofacModule overrideModule = null)
        {
            try
            {
                var builder = new ContainerBuilder();
                
                builder.RegisterModule(new Main(loggerFactory, configuration));
                builder.RegisterModule<Service>();
                builder.RegisterModule<Data>();
                builder.RegisterModule<Reporting>();
                builder.RegisterModule<HtmlEmail>();

                if (!(overrideModule is null))
                {
                    builder.RegisterModule(overrideModule);
                }

                return builder.Build();
            }
            catch (Exception innerException)
            {
                throw new InitializationException(innerException, ExitCode.DependencyInjectionInitializationFault);
            }
        }
    }

}
