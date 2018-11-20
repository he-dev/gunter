using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Components;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachements;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;

namespace Gunter
{
    public class Program : IDisposable
    {
        private readonly IContainer _container;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public Program(IContainer container)
        {
            _container = container;
            _logger = container.Resolve<ILogger<Program>>();
            _configuration = container.Resolve<IConfiguration>();
        }

        public static Program Create() => new Program(ProgramContainerFactory.CreateContainer());

        internal static async Task<int> Main(string[] args)
        {
            try
            {
                using (var program = Create())
                {
                    program.SayHallo();
                    await program.RunAsync();
                    program.SayGoodBye();
                }

                return (int)ExitCode.Success;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return (int)ExitCode.Error;
            }
        }

        private void SayHallo() => _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { Hallo = "Let's find some glitches!" }));

        private void SayGoodBye() => _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { GoodBye = "See you next time!" }));

        public async Task RunAsync()
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var programInfo = _container.Resolve<ProgramInfo>();
            await RunAsync(Path.Combine(currentDirectory, programInfo.TestsDirectoryName));
        }

        public async Task RunAsync(string path)
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var testRunner = scope.Resolve<ITestRunner>();
                await testRunner.RunTestsAsync(path, Enumerable.Empty<string>());
            }
        }

        public void Dispose() => _container.Dispose();
    }

    internal enum ExitCode
    {
        Success = 0,
        Error = 1,
    }
}