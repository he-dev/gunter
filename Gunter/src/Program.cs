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
using Microsoft.Extensions.Configuration;
using Reusable;
using Reusable.Commander;
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Quickey;

namespace Gunter
{
    using static ContainerFactory;

    public class Program : IDisposable
    {
        private readonly IContainer _container;
        private readonly ILogger _logger;
        private readonly ICommandExecutor _commandExecutor;
        private readonly IResourceProvider _resources;
        private readonly ICommandFactory _commandFactory;

        public Program(IContainer container)
        {
            _container = container;
            _logger = container.Resolve<ILogger<Program>>();
            _commandExecutor = container.Resolve<ICommandExecutor>();
            _commandFactory = container.Resolve<ICommandFactory>();
            _resources = container.Resolve<IResourceProvider>();

            var location = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            Directory.SetCurrentDirectory(location);

            var configuration =
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
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

        private void LogHallo() => _logger.Log(Abstraction.Layer.Service().Routine(nameof(Main)).Running(), l => l.Message("G’day!"));

        private void LogGoodBye() => _logger.Log(Abstraction.Layer.Service().Routine(nameof(Main)).Completed(), l => l.Message("See ya!"));

        public async Task RunAsync()
        {
            var defaultTestsDirectoryName = await _resources.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
            var defaultPath = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName);
            await RunAsync($"run -path \"{defaultPath}\"");
        }

        public async Task RunAsync(params string[] args)
        {
            await _commandExecutor.ExecuteAsync<object>(args.Join(" "), default, _commandFactory);
        }

        public void Dispose() => _container.Dispose();
    }

    internal enum ExitCode
    {
        Success = 0,
        Error = 1,
    }
}