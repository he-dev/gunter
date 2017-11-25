using System;
using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Gunter.Modules;
using Reusable.OmniLog;
using Reusable.SmartConfig;
using AutofacModule = Autofac.Module;

namespace Gunter
{
    internal class ProgramModule : AutofacModule
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly AutofacModule _overrideModule;

        public ProgramModule(ILoggerFactory loggerFactory, IConfiguration configuration, AutofacModule overrideModule = null)
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

            var runtimeVariables = new IRuntimeVariable[]
            {
                RuntimeVariable.FromExpression<TestFile>(x => x.FullName),
                RuntimeVariable.FromExpression<TestFile>(x => x.FileName),
                RuntimeVariable.FromExpression<TestCase>(x => x.Level),
                RuntimeVariable.FromExpression<TestCase>(x => x.Message),
                //RuntimeVariable.FromExpression<TestCase>(x => x.Elapsed),
                //RuntimeVariable.FromExpression<Program>(x => x.Environment),
                //RuntimeVariable.FromExpression<Program>(x => x.Name),
                //RuntimeVariable.FromExpression<IDataSource>(x => x.Elapsed)
            };

            builder
                .RegisterInstance(runtimeVariables)
                .As<IEnumerable<IRuntimeVariable>>();

            builder.RegisterModule<ServiceModule>();
            builder.RegisterModule<DataModule>();
            builder.RegisterModule<ReportingModule>();
            builder.RegisterModule<HtmlModule>();

           

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
