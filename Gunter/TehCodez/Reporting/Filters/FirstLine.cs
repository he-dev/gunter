using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Reporting.Filters
{
    internal class FirstLine : IDataFilter
    {
        public object Apply(object data)
        {
            if (!(data is string value)) { throw new ArgumentException($"Invalid data type. Expected {typeof(string).Name} but found {data.GetType().Name}."); }

            return
                string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
    }

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
