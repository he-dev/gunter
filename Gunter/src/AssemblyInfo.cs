using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Gunter;
using Gunter.Reporting;
using Reusable.IOnymous;
using Reusable.SmartConfig;

[assembly:InternalsVisibleTo("Gunter.Tests")]

//[assembly: SettingProvider(SettingNameStrength.Low, nameof(AppSettingProvider), Prefix = "app")]
//[assembly: SettingProvider(SettingNameStrength.Low, nameof(InMemoryProvider))]

[assembly: DebuggerDisplay("{DebuggerDisplay(),nq}", Target = typeof(ColumnMetadata))]