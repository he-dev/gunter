using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Threading.Tasks;
using Autofac;
using Gunter.DependencyInjection;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter
{
    using static ContainerFactory;

    public class Program : IDisposable
    {
        private readonly IContainer _container;
        private readonly ILogger _logger;
        private readonly ICommandLineExecutor _executor;

        public Program(IContainer container)
        {
            _container = container;
            _logger = container.Resolve<ILogger<Program>>();
            _executor = container.Resolve<ICommandLineExecutor>();
        }

        public static Program Create() => new Program(CreateContainer());

        public static Program Create(ILoggerFactory loggerFactory, Action<ContainerBuilder> configureBuilder)
        {
            return new Program(CreateContainer(loggerFactory ?? InitializeLogging(), configureBuilder));
        }

        internal static async Task<int> Main(string[] args)
        {
            try
            {
                using (var program = Create())
                {
                    program.LogHallo();
                    await program.RunAsync();
                    program.LogGoodBye();
                }

                return (int)ExitCode.Success;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return (int)ExitCode.Error;
            }
        }

        private void LogHallo() => _logger.Log(Abstraction.Layer.Service().Meta(new { Hallo = "G’day!" }));

        private void LogGoodBye() => _logger.Log(Abstraction.Layer.Service().Meta(new { GoodBye = "See ya!" }));

        public async Task RunAsync()
        {
            var programInfo = _container.Resolve<ProgramInfo>();
            var defaultPath = Path.Combine(ProgramInfo.CurrentDirectory, programInfo.DefaultTestsDirectoryName);
            await RunAsync($"run -path \"{defaultPath}\"");
        }

        public async Task RunAsync(params string[] args)
        {
            await _executor.ExecuteAsync<object>(args.Join(" "), default);           
        }

        public void Dispose() => _container.Dispose();
    }

    internal enum ExitCode
    {
        Success = 0,
        Error = 1,
    }
}