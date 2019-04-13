using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public SoftString Select { get; set; }

        public SoftString Display { get; set; }

        public bool IsKey { get; set; }

        //[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        //public IFilter Filter { get; set; } = new Unchanged();

        [DefaultValue(ColumnAggregate.Last)]
        public ColumnAggregate Aggregate { get; set; }

        public IFormatter Formatter { get; set; }

        // Not using IList because it's not compatible with the params argument
        public string[] Styles { get; set; }

        private string DebuggerDisplay() => this.ToDebuggerDisplayString(builder =>
        {
            builder.DisplayMember(x => x.Select);
            builder.DisplayMember(x => IsKey);
            //builder.DisplayMember(x => Filter);
            builder.DisplayMember(x => x.Aggregate);
        });

        public bool Equals(ColumnMetadata other) => AutoEquality<ColumnMetadata>.Comparer.Equals(this, other);

        public override bool Equals(object obj) => obj is ColumnMetadata columnOption && Equals(columnOption);

        public override int GetHashCode() => AutoEquality<ColumnMetadata>.Comparer.GetHashCode(this);        
    }
}