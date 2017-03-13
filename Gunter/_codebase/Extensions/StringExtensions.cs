using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Extensions
{
    internal static class StringExtensions
    {
        public static string ToFormatString(this string value) => $"{{{value}}}";
    }
}
