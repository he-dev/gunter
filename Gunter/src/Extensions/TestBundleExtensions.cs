using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Extensions
{
    public static class TestBundleExtensions
    {
        public static IEnumerable<KeyValuePair<SoftString, object>> AllVariables(this TestBundle testBundle) => testBundle.Variables.SelectMany(x => x);

        public static bool IsPartial([NotNull] this TestBundle testBundle)

        {
            if (testBundle == null) throw new ArgumentNullException(nameof(testBundle));
            Debug.Assert(testBundle.FileName.IsNotNullOrEmpty());

            return
                Path
                    .GetFileName(testBundle.FullName)
                    .StartsWith("_", StringComparison.OrdinalIgnoreCase);
        }
    }
}