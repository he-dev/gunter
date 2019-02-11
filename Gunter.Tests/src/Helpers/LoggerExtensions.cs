using System;
using System.Collections.Generic;
using System.Linq;
using Reusable.Extensions;
using Reusable.OmniLog;

namespace Gunter.Tests.Helpers
{
    internal static class LoggerExtensions
    {
        public static IEnumerable<T> Exceptions<T>(this IEnumerable<ILog> logs) where T : Exception
        {
            return
                logs
                    .Select(log => log.Exception<T>())
                    .Where(Conditional.IsNotNull);
        }
    }
}