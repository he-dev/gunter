using System;
using System.Diagnostics;
using Gunter.Reporting.Filters;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Collections;

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

        [AutoEqualityProperty]
        [JsonRequired]
        public SoftString Name { get; set; }

        public bool IsKey { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDataFilter Filter { get; set; } = new Unchanged();

        public ColumnTotal Total { get; set; }

        private string DebuggerDisplay => $"Name = {Name} IsKey = {IsKey} Filter = {Filter?.GetType().Name ?? "null"} Total = {Total}";

        public bool Equals(ColumnOption other) => AutoEquality<ColumnOption>.Comparer.Equals(this, other);

        public override bool Equals(object obj) => obj is ColumnOption columnOption && Equals(columnOption);

        public override int GetHashCode() => AutoEquality<ColumnOption>.Comparer.GetHashCode(this);

        public static implicit operator string(ColumnOption column) => column.Name.ToString();
    }
}