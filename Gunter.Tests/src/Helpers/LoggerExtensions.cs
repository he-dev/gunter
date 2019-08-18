using System;
using System.Collections.Generic;
using System.Linq;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Abstractions.Data;

namespace Gunter.Tests.Helpers
{
    internal static class LoggerExtensions
    {
        public static IEnumerable<T> Exceptions<T>(this IEnumerable<LogEntry> logs) where T : Exception
        {
            return
                logs
                    .Select(log => log.Exception<T>())
                    .Where(Conditional.IsNotNull);
        }

        public static void AssertNone<T>(this IEnumerable<T> exceptions) where T : Exception
        {
            var aex = new AggregateException(exceptions);
            if (aex.InnerExceptions.Any())
            {
                throw aex;
            }
        }
    }
}