using System;
using Autofac;
using Reusable.OmniLog;
using Reusable.SmartConfig;
using DI = Gunter.DependencyInjection;

namespace Gunter.DependencyInjection
{
    public static class ProgramFactory
    {
        public static IContainer CreateProgram(ILoggerFactory loggerFactory, IConfiguration configuration, Autofac.Module testModule = null)
        {
            try
            {
                var builder = new ContainerBuilder();

                builder.RegisterModule(new DI.Program(loggerFactory, configuration));
                builder.RegisterModule<DI.Internal.Service>();
                builder.RegisterModule<DI.Internal.Data>();
                builder.RegisterModule<DI.Internal.Reporting>();
                builder.RegisterModule<DI.Internal.HtmlEmail>();

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