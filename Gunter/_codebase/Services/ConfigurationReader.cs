using Autofac;
using Gunter.Data.Configurations;
using Gunter.Testing;
using Newtonsoft.Json;
using Reusable;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Services
{
    internal static class ConfigurationReader
    {
        private static readonly ILogger _logger = LoggerFactory.CreateLogger(nameof(ConfigurationReader));

        public static IConstantResolver ReadGlobals()
        {
            var fileName = PathResolver.Resolve(AppSettingsConfig.TestsDirectoryName, "Globals.json");

            return
                File.Exists(fileName)
                    ? new ConstantResolver(JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(fileName)))
                        .Add("Environment", AppSettingsConfig.Environment)
                    : ConstantResolver.Empty;
        }

        public static IEnumerable<TestConfiguration> ReadTests(IContainer container)
        {
            var testFileNames = Directory.GetFiles(AppSettingsConfig.TestsDirectoryName, "tests.*.json");


            return testFileNames.Select(LoadTest).Where(test => test != null);

            TestConfiguration LoadTest(string fileName)
            {
                using (var logger = LogEntry.New().AsAutoLog(_logger))
                {
                    try
                    {
                        var json = File.ReadAllText(fileName);
                        var test = JsonConvert.DeserializeObject<TestConfiguration>(json, new JsonSerializerSettings
                        {
                            ContractResolver = new AutofacContractResolver(container),
                            DefaultValueHandling = DefaultValueHandling.Populate,
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                        logger.Message("Imported \"{fileName}\".".Format(new { fileName }));
                        return test;
                    }
                    catch (Exception ex)
                    {
                        logger.Error().Message("Could not import \"{fileName}\".".Format(new { fileName })).Exception(ex);
                        return null;
                    }
                }
            }
        }
    }
}
