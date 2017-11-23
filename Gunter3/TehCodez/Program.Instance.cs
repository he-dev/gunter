using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Reusable;
using Gunter.Data;
using System.Threading.Tasks;
using Gunter.Modules;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Gunter.Services;
using NLog.Fluent;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Binding;
using AutofacModule = Autofac.Module;

namespace Gunter
{
    

    internal partial class Program
    {
        public static readonly string Version = "3.0.0";

        [Required]
        public string Environment { get; }

        [DefaultValue(nameof(Themes))]
        public string Themes { get; }

        public string Targets => Path.Combine(Themes, nameof(Targets));

        public string Name => Assembly.GetAssembly(typeof(Program)).GetName().Name;

        public void Start(string[] args)
        {
            var globalFile = LoadGlobalFile();

            var globals = RuntimeFormatter.Empty
                .MergeWith(globalFile.Globals)
                .MergeWith(_variableBuilder.BuildVariables(this));

            var testFiles = LoadTestFiles().ToList();

            _logger.Log(e => e.Debug().Message($"Test files ({testFiles.Count}) loaded."));
            _logger.Log(e => e.Message($"*** {Name} v{Version} started. ***"));

            _testRunner.RunTestFiles(testFiles, args, globals);
        }
    }

    internal class GunterModule : AutofacModule
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly AutofacModule _overrideModule;

        public GunterModule(ILoggerFactory loggerFactory, IConfiguration configuration, AutofacModule overrideModule = null)
        {
            _loggerFactory = loggerFactory;
            _configuration = configuration;
            _overrideModule = overrideModule;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterInstance(_loggerFactory)
                .As<ILoggerFactory>();

            builder
                .RegisterInstance(_configuration)
                .As<IConfiguration>();

            builder.RegisterModule<SystemModule>();
            builder.RegisterModule<DataModule>();
            builder.RegisterModule<ReportingModule>();
            builder.RegisterModule<HtmlModule>();

            builder
                .RegisterType<TestLoader>()
                .As<ITestLoader>();

            builder
                .RegisterType<TestRunner>()
                .As<ITestRunner>();

            if (!(_overrideModule is null))
            {
                builder.RegisterModule(_overrideModule);
            }

            //Logger.Create<Program>().Log(e => e.Debug().Message("IoC initialized."));
        }
    }

    public class TestConfigurationException : Exception
    {
        public TestConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
