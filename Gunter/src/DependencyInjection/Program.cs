using System;
using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Gunter.DependencyInjection.Internal;
using Reusable.OmniLog;
using Reusable.SmartConfig;

namespace Gunter.DependencyInjection
{
    public class Program : Autofac.Module
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;

        private Program(ILoggerFactory loggerFactory, IConfiguration configuration)
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
                .As(typeof(ILogger<>));

            builder
                .RegisterInstance(_configuration)
                .As<IConfiguration>();

            builder
                .RegisterType<Gunter.Program>()
                .AsSelf();
            
            builder
                .RegisterType<TestFileProvider>()
                .As<ITestFileProvider>();

            builder
                .RegisterType<TestFileSerializer>()
                .As<ITestFileSerializer>();

            builder
                .RegisterInstance(RuntimeVariable.Enumerate());
        }

        public static IContainer Create(ILoggerFactory loggerFactory, IConfiguration configuration, Autofac.Module testModule = null)
        {
            try
            {
                var builder = new ContainerBuilder();
                
                builder.RegisterModule(new Program(loggerFactory, configuration));

                builder.RegisterModule<Service>();
                builder.RegisterModule<Internal.Data>();
                builder.RegisterModule<Internal.Reporting>();
                builder.RegisterModule<HtmlEmail>();

                if (!(testModule is null))
                {
                    builder.RegisterModule(testModule);
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
