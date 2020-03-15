using System;
using System.ComponentModel;
using Gunter.Annotations;
using Gunter.Data.Configuration.Reporting.Abstractions;

namespace Gunter.Services
{
    using static TimeSpanPrecision;
    
    [Gunter]
    public class FormatTimeSpan : IFormatData
    {
        [DefaultValue(@"mm\:ss\.fff")]
        public string Format { get; set; }

        [DefaultValue(Milliseconds)]
        public TimeSpanPrecision Precision { get; set; }

        public string Apply(object? value)
        {
            return value is {} ? TimeSpanFactory(Precision)(Convert.ToDouble(value)).ToString(Format) : default;
        }

        private static Func<double, TimeSpan> TimeSpanFactory(TimeSpanPrecision timeSpanPrecision)
        {
            return timeSpanPrecision switch
            {
                Milliseconds => TimeSpan.FromMilliseconds,
                Seconds => TimeSpan.FromSeconds,
                Minutes => TimeSpan.FromMinutes,
                Hours => TimeSpan.FromHours,
                Days => TimeSpan.FromDays,
                _ => TimeSpanFactory(Milliseconds)
            };
        }
    }

    public enum TimeSpanPrecision
    {
        Milliseconds,
        Seconds,
        Minutes,
        Hours,
        Days
    }
}