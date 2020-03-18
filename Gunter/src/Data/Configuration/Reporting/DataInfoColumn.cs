using System;
using System.ComponentModel;
using Gunter.Data.Configuration.Reporting.Abstractions;
using Newtonsoft.Json;
using Reusable;
using Reusable.Collections;
using Reusable.Diagnostics;

namespace Gunter.Data.Configuration.Reporting
{    
    public class DataInfoColumn : IEquatable<DataInfoColumn>
    {
        [AutoEqualityProperty]
        [JsonRequired]
        public SoftString Select { get; set; }

        public SoftString Display { get; set; }

        public bool IsKey { get; set; }

        [DefaultValue(ColumnAggregate.Last)]
        public ColumnAggregate Aggregate { get; set; }

        public IFormatData? Formatter { get; set; }

        // Not using IList because it's not compatible with the params argument
        public string[] Styles { get; set; }

        private string DebuggerDisplay() => this.ToDebuggerDisplayString(builder =>
        {
            builder.DisplaySingle(x => x.Select);
            builder.DisplaySingle(x => IsKey);
            //builder.DisplayMember(x => Filter);
            builder.DisplaySingle(x => x.Aggregate);
        });

        public bool Equals(DataInfoColumn other) => AutoEquality<DataInfoColumn>.Comparer.Equals(this, other);

        public override bool Equals(object obj) => obj is DataInfoColumn columnOption && Equals(columnOption);

        public override int GetHashCode() => AutoEquality<DataInfoColumn>.Comparer.GetHashCode(this);        
    }
}