using System;
using System.Collections.Generic;
using System.ComponentModel;
using Gunter.Annotations;

namespace Gunter.Reporting.Formatters
{
    [Gunter]
    public class TimeSpan : IFormatter
    {
        private static readonly IDictionary<TimeSpanValueType, Func<double, System.TimeSpan>> ValueTypes = new Dictionary<TimeSpanValueType, Func<double, System.TimeSpan>>
        {
            [TimeSpanValueType.Milliseconds] = System.TimeSpan.FromMilliseconds,
            [TimeSpanValueType.Seconds] = System.TimeSpan.FromSeconds,
            [TimeSpanValueType.Minutes] = System.TimeSpan.FromMinutes,
            [TimeSpanValueType.Hours] = System.TimeSpan.FromHours,
            [TimeSpanValueType.Days] = System.TimeSpan.FromDays,
        };

        [DefaultValue(@"mm\:ss\.fff")]
        public string Format { get; set; }

        [DefaultValue(TimeSpanValueType.Milliseconds)]
        public TimeSpanValueType Type { get; set; }

        public string Apply(object value)
        {
            return
                value is null
                    ? default
                    : ValueTypes[Type](Convert.ToDouble(value)).ToString(Format);
        }
    }

    public enum TimeSpanValueType
    {
        Milliseconds,
        Seconds,
        Minutes,
        Hours,
        Days
    }
}