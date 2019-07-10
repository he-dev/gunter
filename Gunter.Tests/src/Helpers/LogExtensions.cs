using System;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Tests.Helpers
{
    internal static class LogExtensions
    {
        public static T Exception<T>(this ILog log) where T : Exception
        {
            return log.TryGetValue(nameof(Exception), out var obj) && obj is T t ? t : default;
        }

        //public static T PropertyOrDefault<T>(this ILog log, string name) => log.TryGetValue(name, out var value) && value is T actual ? actual : default;
    }
}