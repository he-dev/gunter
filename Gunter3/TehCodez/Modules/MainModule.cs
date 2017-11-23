using System;
using Autofac;
using Gunter.Services;
using Reusable.OmniLog;
using Reusable.SmartConfig;
using AutofacModule = Autofac.Module;

namespace Gunter.Modules
{
    internal class MainModule : AutofacModule
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly AutofacModule _overrideModule;

        public MainModule(ILoggerFactory loggerFactory, IConfiguration configuration, AutofacModule overrideModule = null)
        {
            _loggerFactory = loggerFactory;
            _configuration = configuration;
            _overrideModule = overrideModule;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterInstance(_loggerFactory)
                .As<ILoggerFactory>();

            builder
                .RegisterInstance(_configuration)
                .As<IConfiguration>();

            builder.RegisterModule<SystemModule>();
            builder.RegisterModule<DataModule>();
            builder.RegisterModule<ReportingModule>();
            builder.RegisterModule<HtmlModule>();

            builder
                .RegisterType<TestLoader>()
                .As<ITestLoader>();

            builder
                .RegisterType<TestRunner>()
                .As<ITestRunner>();

            if (!(_overrideModule is null))
            {
                builder.RegisterModule(_overrideModule);
            }

            //Logger.Create<Program>().Log(e => e.Debug().Message("IoC initialized."));
        }
    }

    public class TestConfigurationException : Exception
    {
        public TestConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
