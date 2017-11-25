using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Reusable.IO;

namespace Gunter.Modules
{
    internal class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {


            builder
                .RegisterType<RuntimeFormatter>()
                .As<IRuntimeFormatter>();

            //builder
            //    .RegisterType<PathResolver>()
            //    .As<IPathResolver>();

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