using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Reusable.OmniLog;
using Reusable.SmartConfig;
using AutofacModule = Autofac.Module;

namespace Gunter.Modules
{
    internal class Main : AutofacModule
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly AutofacModule _overrideModule;

        private Main(ILoggerFactory loggerFactory, IConfiguration configuration, AutofacModule overrideModule = null)
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

            // todo add other runtime variables

            var runtimeVariables = new[]
            {
                RuntimeVariable.TestFile.FullName,
                RuntimeVariable.TestFile.FileName,
                RuntimeVariable.TestCase.Level,
                RuntimeVariable.TestCase.Message,
                //RuntimeVariable.FromExpression<TestCase>(x => x.Elapsed),
                //RuntimeVariable.FromExpression<Program>(x => x.Environment),
                RuntimeVariable.Program.FullName,
                RuntimeVariable.Program.Environment,
                //RuntimeVariable.FromExpression<IDataSource>(x => x.Elapsed)
            };

            builder
                .RegisterInstance(runtimeVariables)
                .As<IEnumerable<IRuntimeVariable>>();

            builder.RegisterModule<Modules.Service>();
            builder.RegisterModule<Modules.Data>();
            builder.RegisterModule<Modules.Reporting>();
            builder.RegisterModule<Modules.HtmlEmail>();

            if (!(_overrideModule is null))
            {
                builder.RegisterModule(_overrideModule);
            }
        }

        public static IContainer Create(ILoggerFactory loggerFactory, IConfiguration configuration, AutofacModule overrideModule = null)
        {
            var builder = new ContainerBuilder();

            builder
                    .RegisterModule(new Main(loggerFactory, configuration, overrideModule));

            return builder.Build();
        }
    }
}
