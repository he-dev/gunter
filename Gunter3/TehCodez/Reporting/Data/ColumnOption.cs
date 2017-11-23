using System;
using System.Diagnostics;
using Gunter.Reporting.Filters;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Reporting.Data
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}"), PublicAPI]
    public class ColumnOption : IEquatable<ColumnOption>
    {
        public static readonly ColumnOption GroupCount = new ColumnOption
        {
            Name = "GroupCount",
            Total = ColumnTotal.Count
        };

        [JsonRequired]
        public string Name { get; set; }

        public bool IsKey { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDataFilter Filter { get; set; } = new Unchanged();

        public ColumnTotal Total { get; set; }

        private string DebuggerDisplay => $"Name = {Name} IsKey = {IsKey} Filter = {Filter?.GetType().Name ?? "null"} Total = {Total}";

        public bool Equals(ColumnOption other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ColumnOption)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        }

        public static implicit operator string(ColumnOption column) => column.Name;
    }
}