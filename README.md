#Gunter v1.0.0

`Gunter` the ultimate Glitch Hunter is a tool for _unit-testing_ data and publishing alerts when a test fails.

`Gunter` if ver young and currently it can only query the the `Sql Server` and send email alerts but its abstraction layers allow to easily add other data sources and alerts which I might do in future.

To use `Gunter` you'll need a service that will run it.

##Configuration

### General

 `Gunter` uses `JSON` files for test configuration and needs to know where to find them so you need to specify this path in the `app.config`. The path can be relative or absolute.

 The `app.config` is also used to configure the mail settings.

```xml
<appSettings>

  <add key="Gunter.Paths.TestsDirectoryName" value="_Tests"/>

</appSettings>

  <system.net>
  <mailSettings>
    <smtp deliveryMethod="Network" from="...">
      <network
        host="..."
        port="..."
        enableSsl="true"
        defaultCredentials="false"
        userName="..."
        password="..."
      />
    </smtp>
  </mailSettings>
</system.net>
```

###Tests

Test are configured 

```js
{
  // <object> - Allows to define local constants and/or override globals.
  "Locals": {
    "Log": "[dbo].[Log]",
    "Top": "1000",
    "To": "example@email.com"
  },
  // Data-sources used by the tests.
  "DataSources": [
    {
      "$type": "Gunter.Data.SqlClient.TableOrViewDataSource, Gunter", // <string> - The type specification of the data-source. {no}
      "Id": 1, // <int> - The id of the data-source.
      "ConnectionString": "{TEST_LOG}", // <string> - The connection string. {yes}
      // Specified the commands.
      "Commands": {
        // The Main command queries the data for testing.
        "Main": {
          "Text": "SELECT TOP({Top}) * FROM (SELECT * FROM {Log} WHERE [Environment] = @Environment AND [Timestamp] > DATEADD(HOUR, -1, GETUTCDATE())) AS t", // {yes}
          "Parameters": { "@Environment": "{Environment}" } // {yes}
        },
        // The Debug command can be used to query the data for debugging.
        "Debug": { "Text": "{DebugCommand}" } // {yes}
      }
    }
  ],
  // <object[]> - Test cases.
  "Tests": [
    {
      "Enabled": true, // [bool]
      "Severity": "Warning", // [Warning | Critical] - {no}
      "Message": "Debug logging is on.", // <string> - {yes}
      "DataSources": [ 1 ], // <int[]> - The Id(s) of the data-source(s).
      "Filter": "[LogLevel] IN ('debug')", // [string] - Allows to filter the results. {yes}
      "Expression": "Count([LogLevel]) = 0", // <string> - Must evaluate to boolean. {yes}
      "Assert": true, // [bool] - Specifies whether the result of the expression should be true or false.
      "BreakOnFailure": false, // [bool] - Specifies whether testing can continue if this one fails.
      "Alerts": [ 1 ], // <int[]> - The id(s) of the alert(s).
      "Profiles" : [] // [string[]] - Allows to define profiles and use this test only in specific scenarios.
    }   
  ],
  // <objec[]> - Alerts used by the tests.
  "Alerts": [
    {
      "$type": "Gunter.Alerts.EmailAlert, Gunter", // <string> - Type specification of the alert. {no}
      "Id": 1, // <int> - The id of the alert.
      "Title": "{DataSourceName} - {Severity}", // <string> - {yes}
      "To": "{To}", // <string> - Comma or semicolon separated list of email. {yes}
      // Specifies which sections an alert can contain.
      "Sections": [
        {
          "$type": "Gunter.Alerts.Sections.Text, Gunter", // <string>- The type specification of the section. {no}
          "Heading": "Glitch alert", // <string> - {yes}
          "Text": "{Test.Message}" // [string] - {yes}
        },
        { 
          "$type": "Gunter.Alerts.Sections.DataSourceInfo, Gunter", 
          "Heading": "Data-source"
        }, 
        { 
          "$type": "Gunter.Alerts.Sections.TestInfo, Gunter", 
          "Heading": "Test case"
        }, 
        {
          "$type": "Gunter.Alerts.Sections.DataAggregate, Gunter", 
          "Title": "Exceptions",
          // <string[]> - Columns that the data-aggregate should contain.
          // Columns can be "decorated" with:
          // - [key] - Used to group the results.
          // - [firstline] - Extracts the first line of the value (useful for exception strings).
          // - [min, max, count, sum, avg] - Allosw to aggregate the values, if nothing is specified then "first" is used.
          "Columns": [
            "Timestamp",
            "LogLevel | key",
            "Logger | key",
            "ElapsedSeconds | sum",
            "Message",
            "Exception | key firstline"
          ] // {no}
        }        
      ]
    }
  ]  
}
```
