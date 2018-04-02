using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Reporting.Filters
{
    internal class FirstLine : IDataFilter
    {
        public object Apply(object data)
        {
            if (data is null)
            {
                return null;                
            }

            if (!(data is string value))
            {
                throw new ArgumentException($"Invalid data type. Expected {typeof(string).Name} but found {data.GetType().Name}.");
            }

            return
                string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        public override string ToString()
        {
            return $"{nameof(FirstLine)}";
        }
    }
}
