using System.Diagnostics;
using System.Runtime.CompilerServices;
using Gunter.Data.Configuration.Reporting;

[assembly:InternalsVisibleTo("Gunter.Tests")]

//[assembly: SettingProvider(SettingNameStrength.Low, nameof(AppSettingProvider), Prefix = "app")]
//[assembly: SettingProvider(SettingNameStrength.Low, nameof(InMemoryProvider))]

[assembly: DebuggerDisplay("{DebuggerDisplay(),nq}", Target = typeof(DataInfoColumn))]