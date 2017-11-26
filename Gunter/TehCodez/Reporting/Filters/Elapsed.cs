using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace Gunter.Reporting.Filters
{
    [UsedImplicitly, PublicAPI]
    internal class Elapsed : IDataFilter
    {
        [DefaultValue(@"mm\:ss\.fff")]
        public string Format { get; set; }

        public object Apply(object data)
        {
            switch (data)
            {
                case int elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                case long elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                case float elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                case double elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                default:
                    throw new ArgumentException($"Invalid data type. Expected {typeof(int).Name} or  {typeof(long).Name} but found {data.GetType().Name}.");
            }
        }
    }
}