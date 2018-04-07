using System;
using System.Diagnostics;
using Gunter.Reporting;
using Gunter.Reporting.Data;
using Gunter.Reporting.Filters;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Collections;
using Reusable.Diagnostics;


namespace Gunter.Reporting
{
    public class ColumnOption : IEquatable<ColumnOption>
    {
        public static readonly ColumnOption GroupCount = new ColumnOption
        {
            Name = "RowCount",
            Total = ColumnTotal.Count
        };

        [AutoEqualityProperty]
        [JsonRequired]
        public SoftString Name { get; set; }

        public bool IsKey { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDataFilter Filter { get; set; } = new Unchanged();

        public ColumnTotal Total { get; set; }

        public IFormatter Formatter { get; set; }

        private string DebuggerDisplay() => this.ToDebuggerDisplayString(builder =>
        {
            builder.Property(x => x.Name);
            builder.Property(x => IsKey);
            builder.Property(x => Filter);
            builder.Property(x => x.Total);
        });

        public bool Equals(ColumnOption other) => AutoEquality<ColumnOption>.Comparer.Equals(this, other);

        public override bool Equals(object obj) => obj is ColumnOption columnOption && Equals(columnOption);

        public override int GetHashCode() => AutoEquality<ColumnOption>.Comparer.GetHashCode(this);

        public static implicit operator string(ColumnOption column) => column.Name.ToString();
    }
}