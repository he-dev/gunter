# Gunter v1.0.0

`Gunter` the ultimate Glitch Hunter is a tool for _unit-testing_ data and publishing alerts when a test fails.

`Gunter` is still very young and currently it can only query the the `Sql Server` and send email alerts but its abstraction layers allow to easily add other data sources and alerts which I might do in future.

To use `Gunter` you'll need a service that will run it (like the `Aion` scheduler or you can use the Windows Task Scheduler).

## Configuration

### General

 `Gunter` uses `JSON` files for test configuration and needs to know where to find them so you need to specify their directory name in the `app.config`. The path can be relative or absolute.

 The `app.config` is also used to configure the mail settings.

```xml
<appSettings>

  <add key="gunter.Environment" value="debug" />
  <add key="gunter.Paths.TestsDirectoryName" value="_Tests"/>

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

### Tests

The example below shows all possible settings where `<>` means mandatory values and `[]` optional ones. The value provided for optional settings is the default value. Some settings support string interpolation. Those that do are marked with `{yes}` otherwise if they don't support it you'll fine the `{no}`.

In addition to specific test settings you can use a global dictionary by creating the `Gunter.Globals.json` file in the same directory where the tests are e.g.:

```js
{
  "Global.Key": "Global value"
}
```

The keys can by any string that complies to C# identifier rules with one exception, it may contain a `.`.

Some keys are reserved for internal use:

- `Environment` (can be set via `app.config`)
- `TestConfiguration.FileName`
- `TestCase.Severity`
- `TestCase.Message`
- `TestCase.Profile`

Global values can be overriden with local ones via the `Locals` dictionary (except the reserved ones).

`Gunter` will pick up test files that are named according to this pattern: `Gunter.Tests.*.json`.

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

## Using with `Aion`

If you use `Aion` to run `Gunter` then you need to create an `Aion.Schemes.Gunter.json` file in the `Paths.RobotsDirectoryName` directory e.g.:

```js
{
  "Schedule": "0 0 0/1 1/1 * ? *", // hourly
  "Enabled": true, 
  "StartImmediately": false, 
  "Robots": [
    {
      "FileName": "Gunter.exe", 
      "Enabled": true, 
      "WindowStyle": "Hidden"
    }
  ]
}
```

## Logging

By default `Gunter` uses the Sql Server for logging and would like to find a table like this one:

```sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GunterLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Environment] [nvarchar](53) NOT NULL,
	[Timestamp] [datetime2](7) NOT NULL,
	[LogLevel] [nvarchar](53) NOT NULL,
	[Logger] [nvarchar](103) NOT NULL,
	[ThreadId] [int] NOT NULL,
	[ElapsedSeconds] [float] NULL,
	[Message] [nvarchar](max) NULL,
	[Exception] [nvarchar](max) NULL,
 CONSTRAINT [PK_dbo_GunterLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
```

## Development

You can use create your own private configuration files for testing that will be ignored by repository:

- `_Debug\Debug.private.App.config` 
- `_Test\Gunter.Tests.Gunter.private.json`