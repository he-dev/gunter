using Gunter.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Services.Validators
{
    internal static class TestConfigurationValidator
    {
        public static void ValidateDataSources(TestFile config, ILogger logger)
        {
            var dataSourceIds = config.DataSources.Select(ds => ds.Id);

            foreach (var x in config.Tests.Select((x, i) => new { Test = x, Index = i }))
            {
                foreach (var missingDataSourceId in x.Test.DataSources.Except(dataSourceIds))
                {
                    LogEntry.New().Warn().Message($"Data-source {missingDataSourceId} for test {x.Index} in '{Path.GetFileName(config.FileName)}' not found.").Log(logger);
                }                
            }
        }

        public static void ValidateAlerts(TestFile config, ILogger logger)
        {
            var alertIds = config.Alerts.Select(a => a.Id);

            foreach (var x in config.Tests.Select((x, i) => new { Test = x, Index = i }))
            {
                foreach (var missingAlertId in x.Test.Alerts.Except(alertIds))
                {
                    LogEntry.New().Warn().Message($"Alert {missingAlertId} for test {x.Index} in '{Path.GetFileName(config.FileName)}' not found.").Log(logger);
                }
            }
        }
    }
}
