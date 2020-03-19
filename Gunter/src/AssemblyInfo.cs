using System.Diagnostics;
using System.Runtime.CompilerServices;
using Gunter.Data.Configuration.Reporting;
using Reusable.Diagnostics;

[assembly:InternalsVisibleTo("Gunter.Tests")]

//[assembly: SettingProvider(SettingNameStrength.Low, nameof(AppSettingProvider), Prefix = "app")]
//[assembly: SettingProvider(SettingNameStrength.Low, nameof(InMemoryProvider))]

[assembly: DebuggerDisplay(DebuggerDisplayString.DefaultNoQuotes, Target = typeof(DataColumnSetting))]