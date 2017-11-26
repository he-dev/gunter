using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Reusable.IO;

namespace Gunter.Modules
{
    internal class Service : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<RuntimeFormatterFactory>()
                .As<IRuntimeFormatterFactory>();

            builder
                .Register(c =>
                {
                    var context = c.Resolve<IComponentContext>();
                    return new AutofacContractResolver(context);
                }).SingleInstance();

            builder
                .RegisterType<FileSystem>()
                .As<IFileSystem>();

            builder
                .RegisterType<VariableValidator>()
                .As<IVariableValidator>();

            builder
                .RegisterType<TestLoader>()
                .As<ITestLoader>();

            builder
                .RegisterType<TestRunner>()
                .As<ITestRunner>();
        }
    }
}