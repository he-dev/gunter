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
      "$type": "Gunter.Data.SqlClient.TableOrViewDataSource, Gunter", // {no}
      "Id": 1,
      "Name": "{Log}", // {yes}
      "ConnectionString": "{TEST_LOG}", // [string] - required {yes}
      "Commands": {
        "Main": {
          "Text": "SELECT TOP({Top}) * FROM (SELECT * FROM {Log} WHERE [Environment] = @Environment AND [Timestamp] > DATEADD(HOUR, -1, GETUTCDATE())) AS t", // {yes}
          "Parameters": { "@Environment": "{Environment}" } // {yes}
        },
        "Debug": { "Text": "{DebugCommand}" } // {yes}
      }
    }
  ],
  "Tests": [
    {
      "Enabled": true, // [true | false] - optional - default 'true'
      "Severity": "Warning", // [Warning | Critical] - optional - default 'Warning' {no}
      "Message": "Debug logging is on.", // [string] - requried {yes}
      "DataSources": [ 1 ], // [int[]] - required - The Id(s) of the data-source(s).
      "Filter": "[LogLevel] IN ('debug')", // [string] - optional - Allows to filter the results. {yes}
      "Expression": "Count([LogLevel]) = 0", // [string] - required - Must evaluate to boolean. {yes}
      "Assert": true, // [true | false] - optional - default 'true' - Specifies whether the result of the expression should be true or false.
      "CanContinue": true, // [true | false] - optional - default 'true' - Specifies whether testing can continue if this one fails.
      "Alerts": [ 1 ], // [int[]] - required - The id(s) of the alert(s).
      "Profiles" : [] // [string[]] - optional - allows to define profiles and use this test only in specific scenarios.
    }   
  ],
  "Alerts": [
    {
      "$type": "Gunter.Alerting.Email.EmailAlert, Gunter", // [string] - required - type specification of the alert. {no}
      "Id": 1, // [int[]] - The id of the alert.
      "Title": "{DataSourceName} - {Severity}", // [string] - requried {yes}
      "To": "{To}", // [string] - required, comma or semicolon separated {yes}
      // Specifies which sections an alert can contain.
      "Sections": [
        { "$type": "Gunter.Data.Sections.DataSourceInfo, Gunter" }, // [string] - required - type specification of the section. {no}        
        {
          "$type": "Gunter.Data.Sections.DataAggregate, Gunter", // [string] - required
          // [string[]] - required - Columns that the aggregate summary should contain.
          // Columns can be "decorated" with options: key, firstline, min, max, count, sum, avg
          // key - used to group the results.
          // firstline - extracts the first line of the value (useful for exception strings).
          // min, max, count, sum, avg - allow to aggregate the values, if nothing is specified then "first" is used.
          "Columns": [ "Timestamp", "LogLevel | key", "Logger | key", "ElapsedSeconds | avg", "Message", "Exception | key firstline" ], // {no}
        }        
      ]
    }
  ]  
}
```