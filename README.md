# Gunter v3.0.0

`Gunter` - _the Glitch Hunter_ - is a `Console` tool for searching for abnormalities or glitches in data and publishing messages if something suspicious is found.

Currently it supports only the `Sql Server` and sending emails. Other data sources may added later.

You can use `Gunter` with any task scheduler or run it from the command line.

## Configuration

`Gunter` has three settings in the `app.config`. The `Environment` can be any `string` and the paths can be relative or absolute.

```xml
<appSettings>

	<add key="Environment" value="debug" />
	<add key="TestsDirectoryName" value="C:\..\_Tests" />
	<add key="ThemesDirectoryName" value="C:\..\_Themes" />

</appSettings>
```

In order to send emails the `system.net` section is requried.

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

## Tests

You define your data tests as `JSON` files where you need to specify a couple of sections. 

```json
{
    "Locals": {},
    "DataSources": [],
    "Tests": [],
    "Messages": [],
    "Reports": []
}
```

- `Locals` define variables that can be used in other sections - this is one optional.
- `DataSources` define the queries to select the data to be tested.
- `Tests` define the acutal tests against the data.
- `Messages` define the messages you want to receive.
- `Reports` define what aggregated data messages should contain.

Here are some examples of each section.

Used notation:

- `<>` mandatory setting 
- `[]` optional setting. The value provided for optional settings is the default value
- `{x}` setting supports string interpolation

### `Locals`

```js
  "Locals": {
    "TestingDb": "data source=(local);initial catalog=TestingDb;integrated security=true",
    "Table": "[dbo].[SemLog]"
  },
```

This is a simple `key/value` dictionary.

### `DataSources`

```js
	"DataSources": [
		{
			"$type": "Gunter.Data.SqlClient.TableOrView, Gunter",
			"Id": 150, // <int>
			"ConnectionString": "{TestingDb}", // <string> - {x}
			"Commands": [
				{
					"Name": "Main", // [string]
					"Text": "SELECT * FROM {Table}", // <query>
					"Parameters": {} // [dictionary]
				}
			]
		}
	],
```

This `DataSource` uses the `TableOrView` source. The connection string is defined in the local variable `TestingDb`. Also the `Table` is injected at runtime and there are no other parameters. 

### `Tests`

```js
	"Tests": [
		{
			"Enabled": true, // [bool|true]
			"Level": "Debug", // <LogLevel>
			"Message": "It appears to be something wrong with {Product}.", // <string> - {x}
			"DataSources": [ 150 ], // <int: arrray>
			"Filter": "[LogLevel] IN ('trace', 'debug')", // [string]
			"Expression": "Count([Id]) > 0", // <string>
			"Assert": true, // [bool|true]
			"OnPassed": "Alert, Halt", // [TestActions|None] - Halt|Alert 
			"OnFailed": "Halt", // [TestActions|Alert, Halt]
			"Messages": [ 350 ], // <int: array>
			"Profiles": [] // [string: array]
		},
	]
```

A test needs to define which data-source(s) it uses and which message(s) it's going to trigger. Each test can further filter the data. Internally it uses the `DataTable` so the syntax for both `Filter` and the `Assert` is the same as [DataColumn.Expression Property](https://msdn.microsoft.com/en-us/library/system.data.datacolumn.expression(v=vs.110).aspx) whereas the `Assert` expression must evaluate too `boolean`.

The `Assert` property specifies what is the expected result of the `Expression`. The next two properties `OnPassed` and `OnFailed` specify what should happen if any of these results occur. The engine can either trigger an `Alert` or `Halt` the execution and no other tests are run.

The `Level` is used just for informational purposes and can be used in messages. With `Profiles` you can choose to run only particular tests. If there are no profiles defined the test is always run.

### `Messages`

```js
  "Messages": [
    {
      "$type": "Gunter.Messaging.Emails.HtmlEmail, Gunter",
      "Id": 350, // <int>
      "EmailClient": { "$type": "Reusable.Net.Mail.SmtpClient, Reusable.Net.Mail.SmtpClient" },
      "To": "someone@mail.com", // <string> - ";" separated
      "Theme": "Default.css", // [string|Default.css]
      "Reports": [ 450 ] // <int: array>
    }
  ],
```

Messages specify which report is used as their contents, which theme they should use (optional) and a semicolon `;` separated list of emails.

### `Reports`

```js
  "Reports": [
    {
      "Id": 450,
      "Title": "[{TestCase.Level}] Glitch detected in {Product}",
      "Modules": [
        {
          "$type": "Gunter.Reporting.Modules.Level, Gunter"
        },
        {
          "$type": "Gunter.Reporting.Modules.Greeting, Gunter",
          "Heading": "Hi, everyone.", // [string] - {x}
          "Text": "{TestCase.Message}"
        },
        {
          "$type": "Gunter.Reporting.Modules.TestCase, Gunter",
          "Heading": "Test case" // [string] - {x}
        },
        {
          "$type": "Gunter.Reporting.Modules.DataSource, Gunter",
          "Heading": "Data-source" // [string] - {x}
        },
        {
          "$type": "Gunter.Reporting.Modules.DataSummary, Gunter",
          "Heading": "Data snapshot", // [string] - {x}
          "Columns": [
            { "Name": "Product" },
            {
              "Name": "Transaction",
              "IsKey": true // [bool|false]
            },
            {
              "Name": "Elapsed",
              "Total": "Average", // [ColumnTotal|First] - First, Last, Min, Max, Count, Sum, Average
              "Formatter": { "$type": "Gunter.Reporting.Formatters.Elapsed, Gunter" }
            },
            { "Name": "Event" },
            { "Name": "Result" },
            {
              "Name": "Exception",
              "IsKey": true,
              "Filter": { "$type": "Gunter.Reporting.Filters.FirstLine, Gunter" }
            }
          ]
        },
        {
          "$type": "Gunter.Reporting.Modules.Signature, Gunter"
        }
      ]
    }
  ]
```

Reports specify which modules should be used. Each module can define `Header` and `Text` althoug some might ignore them.

The two modules with most options are:

- `DataSource`
 - By default it tries to use the `Timestamp` column to get the date-time of a row but it can be overriden with the `TimestampColumn` property.
 - If the timestamp column is available then it uses the default formatting `mm\:ss\.fff` for the timespan. You can customize it with `TimespanFormat`.

- `DataSummary`
 - The `Columns` property is a collection of column options for columns that should appear in the report. Their names must correspond to the same data-source columns. Each column can specify whether it's a filter that will be used for grouping. Currently there is only one filter `FirstLine` that is mostly used to extract the message from stack-traces. By default only the `First` line is used from each group. Other options can by specified via the `Total` property which are: `Last`, `Min`, `Max`, `Count`, `Sum`, `Average`. Columns can also use the `Formatter` property to specify a custom formatter for example to render elapsed milliseconds as `mm\:ss\.fff`.

## `_Global.json`

This file contains settings that are shared across multiple tests. They are overriden by tests if specified there. If you have a message or report template that you want to reuse in several tests, you can define it only once here and it'll be merged with the test file. The merge occurs only at the first level.

## Variables

Text fields support a few variables that can be used for example in messages. All variable names are reserved.

- `Program.FullName`
- `Program.Environment`
- `TestFile.FullName`
- `TestFile.FileName`
- `TestCase.Level`
- `TestCase.Message`
- `TestStatistic.GetDataElapsed`
- `TestStatistic.AssertElapsed`

## Logging

Currently `Gunter` uses the `NLog` for writing logs but internally it works with the [`OmniLog`](https://github.com/he-dev/Reusable/tree/master/Reusable.OmniLog) that together with [`SemLog`](https://github.com/he-dev/Reusable/tree/master/Reusable.OmniLog.SemLog) produce a very informative logs.