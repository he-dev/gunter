using Autofac;
using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json.Serialization;
using Reusable.IO;
using Reusable.sdk.Http;
using Reusable.sdk.Mailr;

namespace Gunter.Components
{
    internal class Service : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //builder
            //    .RegisterType<RuntimeFormatterFactory>()
            //    .As<IRuntimeFormatterFactory>();

            builder
                .RegisterType<DirectoryTree>()
                .As<IDirectoryTree>();

            builder
                .RegisterType<PhysicalFileProvider>()
                .As<IFileProvider>();

            builder
                .RegisterType<TestFileSerializer>()
                .As<ITestFileSerializer>();

            builder
                .RegisterInstance(RuntimeVariable.Enumerate());

            builder
                .Register(c =>
                {
                    var context = c.Resolve<IComponentContext>();
                    return new AutofacContractResolver(context);
                }).SingleInstance()
                .As<IContractResolver>();

            builder
                .RegisterType<VariableNameValidator>()
                .As<IVariableNameValidator>();

            builder
                .RegisterType<TestLoader>()
                .As<ITestLoader>();

            builder
                .RegisterType<TestComposer>()
                .As<ITestComposer>();

            builder
                .RegisterType<TestRunner>()
                .As<ITestRunner>();

            builder
                .RegisterType<RuntimeFormatter>()
                .AsSelf();

            builder
                .Register(c =>
                {
                    var programInfo = c.Resolve<ProgramInfo>();
                    return MailrClient.Create(programInfo.MailrBaseUri, headers => headers.AcceptJson().UserAgent(ProgramInfo.Name, ProgramInfo.Version));
                })
                .InstancePerLifetimeScope();
        }
    }
}