using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Reusable.Logging;
using Gunter.Services;
using Gunter.Extensions;
using System.Threading.Tasks;
using Gunter.Reporting;

namespace Gunter.Services
{
    internal class TestRunner
    {
        private readonly ILogger _logger;

        public TestRunner(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

#if DEBUG
            MaxDegreeOfParallelism = 1;
#endif
        }

        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        public void RunTests(IEnumerable<TestFile> testFiles, IConstantResolver constants)
        {
            var tests =
                (from testFile in testFiles
                 select TestComposer.ComposeTests(testFile, constants)).ToList();

            LogEntry.New().Debug().Message($"Test configuration count: {tests.Count}").Log(_logger);

            Parallel.ForEach
            (
                source: tests,
                parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                body: RunTest
            );
        }

        public void RunTest(IEnumerable<TestConfiguration> tests)
        {
            foreach (var context in tests)
            {
                var logEntry =
                    LogEntry
                        .New()
                        .SetValue(nameof(TestCase.Expression), context.Test.Expression)
                        .SetValue(VariableName.TestCollection.FileName, context.Constants.Resolve(VariableName.TestCollection.FileName.ToFormatString()));

                foreach (var dataSource in context.DataSources)
                {
                    try
                    {
                        using (var data = dataSource.GetData(context.Constants))
                        {
                            var testCaseContext = new TestContext(context, dataSource, data);

                            if (data.Compute(context.Test.Expression, context.Test.Filter) is bool testResult)
                            {
                                var success = testResult == context.Test.Assert;

                                logEntry.Info().Message($"Assert: {success}");

                                var mustAlert =
                                    (success && context.Test.AlertTrigger == AlertTrigger.Success) ||
                                    (!success && context.Test.AlertTrigger == AlertTrigger.Failure);

                                if (mustAlert)
                                {
                                    foreach (var alert in context.Alerts) alert.Publish(testCaseContext);
                                }

                                if (!success && !context.Test.ContinueOnFailure)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException($"Test expression must evaluate to {nameof(Boolean)}.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logEntry
                            .Error()
                            .Exception(ex)
                            .Message($"Inconclusive. The expression must evaluate to {nameof(Boolean)}.");
                    }
                    finally
                    {
                        logEntry.Log(_logger);
                    }
                }
            }
        }
    }
}
