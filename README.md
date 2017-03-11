# Gunter
Glitch Hunter

```json
{
  // Allows to define local constants and override globals.
  "Locals": {
    "Log": "[dbo].[Log]",
    "Top": "1000",
    "To": "example@email.com"
  },
  // Allows to define data-sources.
  "DataSources": [
    {
      "$type": "Gunter.Data.SqlClient.TableOrViewDataSource, Gunter",
      "Id": 1,
      "Name": "{Log}",
      "ConnectionString": "{TEST_LOG}", // [string] - required
      "Commands": {
        "Main": {
          "Text": "SELECT TOP({Top}) * FROM (SELECT * FROM {Log} WHERE [Environment] = @Environment AND [Timestamp] > DATEADD(HOUR, -1, GETUTCDATE())) AS t",
          "Parameters": { "@Environment": "{Environment}" }
        },
        "Debug": { "Text": "{DebugCommand}" }
      }
    }
  ],
  "Tests": [
    {
      "Enabled": true, // [true | false] - optional - default 'true'
      "Severity": "Warning", // [Warning | Critical] - optional - default 'Warning'
      "Message": "Debug logging is on.", // [string] - requried
      "DataSources": [ 1 ], // [int[]] - required - The Id(s) of the data-source(s).
      "Filter": "[LogLevel] IN ('debug')", // [string] - optional - Allows to filter the results.
      "Expression": "Count([LogLevel]) = 0", // [string] - required - Must evaluate to boolean.
      "Assert": true, // [true | false] - optional - default 'true' - Specifies whether the result of the expression should be true or false.
      "CanContinue": true, // [true | false] - optional - default 'true' - Specifies whether testing can continue if this one fails.
      "Alerts": [ 1 ] // [int[]] - required - The id(s) of the alert(s).
    }   
  ],
  "Alerts": [
    {
      "$type": "Gunter.Alerting.Email.EmailAlert, Gunter", // [string] - required - type specification of the alert.
      "Id": 1, // [int[]] - The id of the alert.
      "Title": "{DataSourceName} - {Severity}", // [string] - requried
      "To": "{To}", // [string] - required, comma or semicolon separated
      // Specifies which sections an alert can contain.
      "Sections": [
        { "$type": "Gunter.Data.Sections.DataSourceSummary, Gunter" }, // [string] - required - type specification of the section.
        //{ "$type": "Gunter.Data.Sections.TestSummary, Gunter" },
        {
          "$type": "Gunter.Data.Sections.ExceptionSummary, Gunter", // [string] - required
          "Columns": [ "Timestamp", "LogLevel", "Logger", "Message", "Exception" ], // [string[]] - required - Columns that the exception summary should contain.
          "GroupBy": [ "Logger", "Message", "Exception" ], // [string[]] - required - Columns that the exception should be grouped upon.
        },
         {
          "$type": "Gunter.Data.Sections.AggregatedSummary, Gunter", // [string] - required
          "Columns": [ "Timestamp", "LogLevel | key", "Logger | key", "ElapsedSeconds | sum", "Message", "Exception | key firstline" ], // [string[]] - required - Columns that the exception summary should contain.
        }        
      ]
    }
  ],
  "Profiles": {
    "normal": [],
    "debug": [ "CheckDebugLevelEnabled" ]
  }
}
```