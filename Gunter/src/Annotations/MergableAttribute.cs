using System;

namespace Gunter.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class MergableAttribute : Attribute
    {
        public bool Required { get; set; }
    }
}