using System;

namespace Gunter.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class MergeableAttribute : Attribute
    {
        public bool Required { get; set; }
    }
}