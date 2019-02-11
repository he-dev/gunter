using System;
using Reusable.OmniLog;

namespace Gunter.Tests.Helpers
{
    internal static class LogExtensions
    {
        public static T Exception<T>(this ILog log) where T : Exception => log.Property<T>();

        //public static T PropertyOrDefault<T>(this ILog log, string name) => log.TryGetValue(name, out var value) && value is T actual ? actual : default;
    }
}