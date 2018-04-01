using Autofac;
using Gunter.Services;
using Gunter.Tests.Messaging;
using Gunter.Tests.Services;

namespace Gunter.Tests.AutofacModules
{
    internal class OverrideModule : Module
    {
        public TestFileSystem FileSystem { get; set; }

        public TestAlert TestAlert { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterInstance(TestAlert);

            builder
                .RegisterType<TestPathResolver>()
                .As<IPathResolver>();

            builder
                .RegisterInstance(FileSystem)
                .As<IFileSystem>();
        }
    }
}