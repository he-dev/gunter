using System;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Abstractions.Data;

namespace Gunter.Tests.Helpers
{
    internal static class LogExtensions
    {
        public static T Exception<T>(this LogEntry log) where T : Exception
        {
            return log.TryGetItem<object>(LogEntry.Names.Exception, LogEntry.Tags.Loggable, out var obj) && obj is T t ? t : default;
        }

        //public static T PropertyOrDefault<T>(this ILog log, string name) => log.TryGetValue(name, out var value) && value is T actual ? actual : default;
    }
}