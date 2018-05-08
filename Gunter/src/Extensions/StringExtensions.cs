﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Extensions
{
    internal static class StringExtensions
    {
        public static string FormatPartialName(this string input)
        {
            return $"_{input.TrimStart('_')}";
        }
    }
}