using System;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Threading.Tasks;
using Autofac;
using Gunter.DependencyInjection;
using Reusable.Commander;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;

namespace Gunter
{
    using static ContainerFactory;

    public class Program : IDisposable
    {
        private readonly IContainer _container;
        private readonly ILogger _logger;
        private readonly ICommandExecutor _commandExecutor;
        private readonly IResource _resource;

        public Program(IContainer container)
        {
            _container = container;
            _logger = container.Resolve<ILogger<Program>>();
            _commandExecutor = container.Resolve<ICommandExecutor>();
            _resource = container.Resolve<IResource>();

            var location = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            Directory.SetCurrentDirectory(location);
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
                    await program.RunAsync(args);
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

        private void LogHallo() => _logger.Log(Abstraction.Layer.Service().Routine(nameof(Main)).Running(), l => l.Message("G’day!"));

        private void LogGoodBye() => _logger.Log(Abstraction.Layer.Service().Routine(nameof(Main)).Completed(), l => l.Message("See ya!"));

//        public async Task RunAsync()
//        {
//            var defaultTestsDirectoryName = await _resources.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
//            var defaultPath = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName);
//            await RunAsync($"run -path \"{defaultPath}\"");
//        }

        public async Task RunAsync(params string[] args)
        {
            // The first argument is the exe.
            args = args.Skip(1).ToArray();
            if (!args.Any())
            {
                var defaultTestsDirectoryName = await _resource.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
                var defaultPath = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName);
                args = new[] { "run", "--path", $"\"{defaultPath}\"" };
            }

            await _commandExecutor.ExecuteAsync<object>(args.Join(" "));
        }

        public void Dispose() => _container.Dispose();
    }

    internal enum ExitCode
    {
        Success = 0,
        Error = 1,
    }
}