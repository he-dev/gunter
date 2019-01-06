using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Gunter;
using Gunter.Reporting;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Annotations;

[assembly:InternalsVisibleTo("Gunter.Tests")]

[assembly: SettingProvider(typeof(AppSettings), Prefix = "app", SettingNameStrength = SettingNameStrength.Low, AssemblyType = typeof(Program))]
[assembly: SettingProvider(typeof(InMemory), SettingNameStrength = SettingNameStrength.Low)]

[assembly: DebuggerDisplay("{DebuggerDisplay(),nq}", Target = typeof(ColumnMetadata))]