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
        public static IEnumerable<TestBundleVariable> Flatten(this IEnumerable<TestBundleVariableCollection> variables)
        {
            return variables.SelectMany(x => x);
        }
    }
}