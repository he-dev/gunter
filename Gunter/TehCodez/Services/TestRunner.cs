using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Alerts;
using Reusable.Logging;
using Gunter.Services;
using Gunter.Extensions;
using System.Threading.Tasks;

namespace Gunter.Services
{
    internal class TestRunner
    {
        private readonly ILogger _logger;

        public TestRunner(ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void RunTests(IEnumerable<TestConfiguration> testConfigs, IConstantResolver constants)
        {
            var testGroups = testConfigs.Select(testConfig => TestComposer.ComposeTests(testConfig, constants));

#if DEBUG
            const int maxDegreeOfParallelism = 1;
#else
            const int maxDegreeOfParallelism = Environment.ProcessorCount;
#endif

            Parallel.ForEach
            (
                source: testGroups,
                parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                body: RunTest
            );
        }

        public void RunTest(IEnumerable<TestContext> tests)
        {
            foreach (var context in tests)
            {
                var logEntry = LogEntry.New()
                    .SetValue(nameof(TestCase.Expression), context.Test.Expression)
                    .SetValue(VariableName.TestConfiguration.FileName, context.Constants.Resolve(VariableName.TestConfiguration.FileName.ToFormatString()));

                try
                {
                    var result = context.Data.Compute(context.Test.Expression, context.Test.Filter) is bool testResult && testResult == context.Test.Assert;

                    switch (result)
                    {
                        case true:
                            logEntry.Info().Message("Passed.");
                            break;

                        case false:
                            logEntry.Error().Message("Failed.");
                            foreach (var alert in context.Alerts) alert.Publish(context);
                            if (context.Test.BreakOnFailure) return;
                            break;

                        default:
                            throw new InvalidOperationException($"Test expression must evaluate to {typeof(bool).Name}.");
                    }
                }
                catch (Exception ex)
                {
                    logEntry
                        .Error()
                        .Exception(ex)
                        .Message($"Inconclusive. The expression must evaluate to {typeof(bool).Name}.");
                }
                finally
                {
                    context.Dispose();
                    logEntry.Log(_logger);
                }
            }
        }
    }
}
