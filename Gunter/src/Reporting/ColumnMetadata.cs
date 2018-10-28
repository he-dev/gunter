using System;
using System.ComponentModel;
using System.Diagnostics;
using Gunter.Reporting;
using Gunter.Reporting.Filters;
using Gunter.Reporting.Filters.Abstractions;
using Gunter.Reporting.Formatters.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Collections;
using Reusable.Diagnostics;


namespace Gunter.Reporting
{    
    public class ColumnMetadata : IEquatable<ColumnMetadata>
    {
        [AutoEqualityProperty]
        [JsonRequired]
        public SoftString Name { get; set; }

        public SoftString Other { get; set; }

        public bool IsGroupKey { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IFilter Filter { get; set; } = new Unchanged();

        [DefaultValue(ColumnTotal.Last)]
        public ColumnTotal Total { get; set; }

        public IFormatter Formatter { get; set; }

        private string DebuggerDisplay() => this.ToDebuggerDisplayString(builder =>
        {
            builder.Property(x => x.Name);
            builder.Property(x => IsGroupKey);
            builder.Property(x => Filter);
            builder.Property(x => x.Total);
        });

        public bool Equals(ColumnMetadata other) => AutoEquality<ColumnMetadata>.Comparer.Equals(this, other);

        public override bool Equals(object obj) => obj is ColumnMetadata columnOption && Equals(columnOption);

        public override int GetHashCode() => AutoEquality<ColumnMetadata>.Comparer.GetHashCode(this);        
    }
}