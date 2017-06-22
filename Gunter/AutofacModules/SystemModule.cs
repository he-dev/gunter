using Autofac;
using Gunter.Data;
using Gunter.Services;

namespace Gunter.AutofacModules
{
    internal class SystemModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            builder
                .RegisterInstance(new VariableBuilder()
                    .AddVariables<TestFile>(
                        x => x.FullName,
                        x => x.FileName)
                    .AddVariables<IDataSource>(
                        x => x.Elapsed)
                    .AddVariables<TestCase>(
                        x => x.Severity,
                        x => x.Message,
                        x => x.Elapsed)
                    .AddVariables<Workspace>(
                        x => x.Environment,
                        x => x.AppName))
                .As<IVariableBuilder>();

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