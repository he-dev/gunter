# Gunter v2.0.0

`Gunter` _the ultimate Glitch Hunter_ is a tool for testing data and publishing alerts when a test either fails or passes (this is new in v2).

`Gunter` is still very young and currently it can only query the the `Sql Server` and send email alerts but its abstraction layers allow to easily add other data sources and alerts which I might do in future.

To use `Gunter` you'll need a service that will run it (like `Aion` or you can use the Windows Task Scheduler).

## Configuration

You need to provide two setting via the `app.config`. The `Environment` that will appear in the logs and the `Workspace.Assets` where `Guten` will be searching for tests (that does not start with an `_`) and stylesheets. This is the subfolder in `Gunter`'s installation folder.

```xml
  <appSettings>

    <add key="Environment" value="debug" />
    <add key="Workspace.Assets" value="assets"/>

  </appSettings>
```

In order to be able to send emails `Gunter` needs to know the smtp settings.

```xml
  <system.net>
    <mailSettings>
      <smtp deliveryMethod="Network" from="YOUR_EMAIL">
        <network
          host="YOUR_HOST"
          port="YOUR_PORT"
          enableSsl="true"
          defaultCredentials="false"
          userName="YOUR_USERNAME"
          password="YOUR_PASSWORD"
        />
      </smtp>
    </mailSettings>
  </system.net>
```

#Tests

 `Gunter` uses `JSON` files for test configuration. The main file is the `_Globals.json` which is a dictionary for global variables. You can use them in tests for example for connection strings. Other `JSON` files in the `assets\targets` forlder are your test files. You can use here not only the global variables but also local ones that can override the globals. To use them just put the name inside `{}`. Some names are reserved and you may not use them for you own variables but you can use them in strings. The complete list is:

- `TestFile.FullName`
- `TestFile.FileName`
- `DataSource.Elapsed`
- `TestCase.Severity`
- `TestCase.Message`
- `TestCase.Elapsed`
- `Workspace.Environment`
- `Workspace.AppName`

The example below shows all possible settings where `<>` means mandatory values and `[]` optional ones. The value provided for optional settings is the default value. Some settings support string interpolation. Those that do are marked with `{yes}` otherwise if they don't support it you'll find the `{no}`.

```js
{
  // <object> - Allows to define local constants and/or override globals.
	"Locals": {
		"TestLog": "[dbo].[Gunter_TestLog]",
	}, 
  // Data-sources used by the tests.
  "DataSources": [
		{
			"$type": "Gunter.Data.SqlClient.TableOrView, Gunter", // <string> - The type specification of the data-source. {no}
			"Id": 1, // <int> - The id of the data-source.
			"ConnectionString": "{TEST_LOG}", // <string> - The connection string. {yes}
      // Specifies commands.
			"Commands": [
				{
          // The Main command is mandatory. Other commands can be defined but only the Main one will be used for quering.
					"Name": "Main",
					"Text": "SELECT * FROM {TestLog}",
          // [dictionary] - Allows to specify SqlCommand parameters.
					"Parameters": { "@Environment": "{Workspace.Environment}" } // {yes}
				}
			]
		}
	],
  // <object[]> - Test cases.
  "Tests": [
    {
      "Enabled": true, // [bool]
      "Severity": "Warn", // [Debug | Info | Warn | Error | Fatal] - {no}
      "Message": "Debug logging is on.", // <string> - {yes}
      "DataSources": [ 1 ], // <int[]> - The Id(s) of the data-source(s).
      "Filter": "[LogLevel] IN ('debug')", // [string] - Allows to filter the results. {yes}
      "Expression": "Count([LogLevel]) = 0", // <string> - Must evaluate to boolean. {yes}
      "Assert": true, // [bool] - Specifies whether the result of the expression should be true or false.
      "OnPassed": "None", // <string> [None | Halt | Alert] - Specifies the action when the test passes.
      "OnFailed": "Alert, Halt", // <string> [None | Halt | Alert] - Specifies the action when the test failes.
      "Alerts": [ 1 ], // <int[]> - The id(s) of the alert(s).
      "Profiles" : [] // [string[]] - Allows to define profiles and use this test only in specific scenarios.
    }   
  ],
  // <objec[]> - Alerts used by the tests.
  "Alerts": [
		{
			"$type": "Gunter.Messaging.Email.HtmlEmail, Gunter", // <string> - Type specification of the alert. {no}
			"Id": 1, // <int> - The id of the alert.
			"EmailClient": { "$type": "Reusable.EmailClients.SmtpClient, Reusable.EmailClients.SmtpClient" }, // EmailClient
			"Theme": "Default.css", // <string> - Email theme.
			"Reports": [1] // <int[]> - Specifies the reports that should be sent.
		}
	],
  // <object> - Reports used for alerting. Each module is optional and can be removed.
	"Reports": [
		{
			"Id": 1, // <int> - Report id
			"Title": "Glitch alert for {TestLog} - {TestCase.Severity}", // <string> - The title {yes}
			"Modules": [
				{
					"$type": "Gunter.Reporting.Modules.Greeting, Gunter",
					"Heading": "Glitch detected!", // <string> - {yes}
					"Text": "{TestCase.Message}" // <string> - {yes}
				},
				{
					"$type": "Gunter.Reporting.Modules.TestCaseInfo, Gunter",
					"Heading": "Test case" // <string> - {yes}
				},
				{
					"$type": "Gunter.Reporting.Modules.DataSourceInfo, Gunter",
					"Heading": "Data-source", // <string> - {yes}
					"TimestampColumn": "Timestamp", // [string] - Timespan column for statistics.
					"TimespanFormat": "dd\\.hh\\:mm\\:ss" // [string] - Custom timespan formatting.
				},
				{
					"$type": "Gunter.Reporting.Modules.DataSummary, Gunter",
					"Heading": "Data summary", // <string> - {yes}
          // <object> - Specifies columns for aggregation.
					"Columns": [
						{
							"Name": "_nvarchar", // <string> - The name of the column. {no}
							"IsKey": true, // [bool] - Used as a key for grouping.
							"Filter": { "$type": "Gunter.Reporting.Filters.FirstLine, Gunter" } // <object> - Allows to specify custom data filter. Currently there is only one "FirstLine"
						},
            // Each column can use one of the following aggregates: Sum, Count, Average, Min, Max, First, Last
						{ "Name": "_int", "Total": "Sum" },
						{ "Name": "_datetime2" },
						{ "Name": "_float", "Total": "Average" },
						{ "Name": "_bit", "Total": "Count" },
						{ "Name": "_money" },
						{ "Name": "_numeric" }
					]
				},
				{
					"$type": "Gunter.Reporting.Modules.Signature, Gunter"
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