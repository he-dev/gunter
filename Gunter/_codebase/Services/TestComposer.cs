using System.Collections.Generic;
using System.Linq;
using System;
using Gunter.Services;
using System.Data;
using Gunter.Data;

namespace Gunter.Services
{
    internal class TestComposer
    {
        public static IEnumerable<TestContext> ComposeTests(TestConfiguration config, IConstantResolver constants)
        {
            var profileExists = constants.TryGetValue(Globals.Profile, out object profile);
            var results =
                from test in config.Tests
                where
                    test.Enabled &&
                    (!profileExists || test.Profiles.Contains((string)profile, StringComparer.OrdinalIgnoreCase))
                from dsId in test.DataSources
                join ds in config.DataSources on dsId equals ds.Id
                select new TestContext
                {
                    Test = test,
                    DataSource = ds,
                    Alerts =
                        from aId in test.Alerts
                        join a in config.Alerts on aId equals a.Id
                        select a,
                    Constants =
                        constants
                            .UnionWith(config.Locals)
                            .Add(Globals.Test.FileName, config.FileName)
                            .Add(Globals.Test.Severity, test.Severity)
                            .Add(Globals.Test.Message, test.Message)
                };
            return results;
        }
    }
}
