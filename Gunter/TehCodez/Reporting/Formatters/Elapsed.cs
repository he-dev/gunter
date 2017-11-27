﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Gunter.Reporting.Formatters
{
    public class Elapsed : IFormatter
    {
        [DefaultValue(@"mm\:ss\.fff")]
        public string Format { get; set; }

        public string Apply(object value)
        {
            switch (value)
            {
                case int elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                case long elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                case float elapsed when float.IsNaN(elapsed):
                    return string.Empty;

                case float elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                case double elapsed when double.IsNaN(elapsed):
                    return string.Empty;

                case double elapsed:
                    return TimeSpan.FromMilliseconds(elapsed).ToString(Format);

                default:
                    throw new ArgumentException($"Invalid data type. Expected {typeof(int).Name} or  {typeof(long).Name} but found {value.GetType().Name}.");
            }
        }
    }
}
