using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Alerts;
using Reusable.Logging;
using Gunter.Services;
using Gunter.Extensions;

namespace Gunter.Services
{
    internal class TestRunner
    {
        private readonly ILogger _logger;

        public TestRunner(ILogger logger)
        {
            _logger = logger;
        }

        public void RunTests(IEnumerable<TestConfiguration> testConfigs, IConstantResolver constants)
        {
            foreach (var context in testConfigs.Select(testConfig => testConfig.ComposeTests(constants)).SelectMany(tests => tests))
            {
                RunTest(context);
            }
        }

        public void RunTest(TestContext context)
        {
            using (var data = context.DataSource.GetData(context.Constants))
            using (var logEntry = LogEntry.New().SetValue(Globals.Test.FileName, context.Constants.Resolve(Globals.Test.FileName.ToFormatString())).AsAutoLog(_logger))
            {
                try
                {
                    var result = data.Compute(context.Test.Expression, context.Test.Filter) is bool testResult && testResult == context.Test.Assert;

                    switch (result)
                    {
                        case true:
                            logEntry.Info().Message("Passed.");
                            break;

                        case false:
                            logEntry.Error().Message("Failed.");

                            foreach (var alert in context.Alerts) alert.Publish(context);

                            if (!context.Test.CanContinue) return;
                            break;

                        default:
                            throw new InvalidOperationException($"Test result must be a {typeof(bool).Name}.");
                    }
                }
                catch (Exception ex)
                {
                    logEntry
                        .Error()
                        .Exception(ex)
                        .Message($"Inconclusive. The expression must evaluate to {typeof(bool).Name}.")
                        .SetValue(Globals.Test.FileName, context.Constants.Resolve(Globals.Test.FileName.ToFormatString()))
                        .SetValue(nameof(TestProperties.Expression), context.Test.Expression);
                }
            }
        }
    }
}
