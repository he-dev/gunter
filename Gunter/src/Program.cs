using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Threading.Tasks;
using Autofac;
using Gunter.ComponentSetup;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter
{
    using static ProgramContainerFactory;

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

        private void LogHallo() => _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { Hallo = "Let's find some glitches!" }));

        private void LogGoodBye() => _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { GoodBye = "See you next time!" }));

        public async Task RunAsync()
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var programInfo = _container.Resolve<ProgramInfo>();
            await RunAsync($"batch -path \"{Path.Combine(currentDirectory, programInfo.DefaultTestsDirectoryName)}\"");
        }

        public async Task RunAsync(params string[] args)
        {
            await _executor.ExecuteAsync(args.Join(" "));           
        }

        public void Dispose() => _container.Dispose();
    }

    internal enum ExitCode
    {
        Success = 0,
        Error = 1,
    }
}