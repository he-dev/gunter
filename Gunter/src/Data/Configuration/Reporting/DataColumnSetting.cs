using System.Collections.Generic;
using System.ComponentModel;
using Gunter.Data.Abstractions;
using Newtonsoft.Json;
using Reusable.Diagnostics;

namespace Gunter.Data.Configuration.Reporting
{    
    public class DataColumnSetting 
    {
        private string? _display;

        [JsonRequired]
        public string Select { get; set; } = default!;

        public string Display
        {
            get => _display ?? Select;
            set => _display = value;
        }

        public bool IsKey { get; set; }

        [DefaultValue(ReduceType.Last)]
        public ReduceType ReduceType { get; set; }

        public IFormatData? Formatter { get; set; }
        
        public List<string> Tags { get; set; } = new List<string>();

        private string DebuggerDisplay() => this.ToDebuggerDisplayString(builder =>
        {
            builder.DisplaySingle(x => x.Select);
            builder.DisplaySingle(x => IsKey);
            //builder.DisplayMember(x => Filter);
            builder.DisplaySingle(x => x.ReduceType);
        });
    }
}