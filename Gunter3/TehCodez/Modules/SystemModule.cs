using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Gunter.Services;

namespace Gunter.Modules
{
    internal class SystemModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            var runtimeVariables = new IRuntimeVariable[]
            {
                RuntimeVariable.FromExpression<TestFile>(x => x.FullName),
                RuntimeVariable.FromExpression<TestFile>(x => x.FileName),
                RuntimeVariable.FromExpression<TestCase>(x => x.Severity),
                RuntimeVariable.FromExpression<TestCase>(x => x.Message),
                //RuntimeVariable.FromExpression<TestCase>(x => x.Elapsed),
                //RuntimeVariable.FromExpression<Program>(x => x.Environment),
                //RuntimeVariable.FromExpression<Program>(x => x.Name),
                //RuntimeVariable.FromExpression<IDataSource>(x => x.Elapsed)
            };
            
            builder
                .RegisterInstance(runtimeVariables)
                .As<IEnumerable<IRuntimeVariable>>();

            builder
                .RegisterType<PathResolver>()
                .As<IPathResolver>();

            builder
                .Register(c =>
                {
                    var context = c.Resolve<IComponentContext>();
                    return new AutofacContractResolver(context);
                }).SingleInstance();

            builder
                .RegisterType<FileSystem>()
                .As<IFileSystem>();
        }
    }
}