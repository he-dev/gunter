using Gunter.Data;
using Gunter.Data.Sections;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;

namespace Gunter.Alerts.Sections
{
    public class DataAggregate : SectionFactory
    {
        private delegate double AggregateCallback(IEnumerable<DataRow> aggregate, string columnName);

        private static readonly Dictionary<string, AggregateCallback> Aggregates = new Dictionary<string, AggregateCallback>(StringComparer.OrdinalIgnoreCase)
        {
            [Column.Option.Min] = (rows, column) => rows.Min(row => row.Field<double>(column)),
            [Column.Option.Max] = (rows, column) => rows.Max(row => row.Field<double>(column)),
            [Column.Option.Count] = (rows, column) => rows.Count(),
            [Column.Option.Sum] = (rows, column) => rows.Where(row => row[column] != DBNull.Value).Sum(row => row.Field<double>(column)),
            [Column.Option.Avg] = (rows, column) => rows.Average(row => row.Field<double>(column)),
        };

        public DataAggregate(ILogger logger) : base(logger) { }

        [JsonRequired]
        public List<string> Columns { get; set; }

        protected override ISection CreateCore(TestContext context)
        {
            var columns = Columns.Select(Column.Parse).ToList();

            // Get only keyed columns.
            var keys = columns.Where(x => x.Options.Contains(Column.Option.Key)).ToList();

            // Group-by keyed columns.
            var groups = context.Data.Select(context.Test.Filter).GroupBy(x =>
                keys.ToDictionary(
                    column => column.Name,
                    column =>
                    {
                        // Get either the field's value or its first line.
                        var value = x.Field<string>(column.Name);
                        return column.Options.Contains(Column.Option.FirstLine) ? GetFirstLine(value) : value;
                    },
                    StringComparer.OrdinalIgnoreCase
                ),
                new DictionaryComparer<string, string>()
            ).ToList();

            // Creates a data-table with the specified columns.
            var body = new DataTable(nameof(DataAggregate));
            foreach (var column in columns) body.AddColumn(column.Name, c => c.DataType = typeof(string));

            // Create aggregated rows and add them to the final data-table.
            var rows = groups.Select(CreateRow);
            foreach (var row in rows) body.Rows.Add(row);

            var footer = new DataTable(nameof(TableSection.Footer));
            foreach (var column in columns) footer.AddColumn(column.Name, c => c.DataType = typeof(string));
            footer.AddRow(columns.Select(column =>
            {
                var options = string.Join(", ", column.Options);
                return (string.IsNullOrEmpty(options) ? Column.Option.First : options).ToLower();
            })
            .ToArray());

            return new TableSection
            {
                Heading = Heading,
                Body = body,
                Footer = footer,
                Orientation = Orientation.Horizontal
            };

            DataRow CreateRow(IGrouping<IDictionary<string, string>, DataRow> group)
            {
                return SetValues(body.NewRow());

                DataRow SetValues(DataRow row)
                {
                    foreach (var column in columns)
                    {
                        // Try to get an aggregate function.
                        var aggregate = Aggregates.FirstOrDefault(x => column.Options.Contains(x.Key));
                        row[column.Name] =
                            aggregate.Key == null
                                // If there is no aggregate function then either get the key value for this column or the first row from the group.
                                ? group.Key.TryGetValue(column.Name, out string value) ? value : group.First().Field<object>(column.Name)
                                // Calculate the aggregate.
                                : aggregate.Value(group, column.Name);
                    }
                    return row;
                }
            }
        }

        private static string GetFirstLine(string value)
        {
            // Extracts the first line. Currently used for exception message that is always the first line of an exception string.
            return
                string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        // Represents an aggregated column and its options.
        private class Column
        {
            private Column(string name, IEnumerable<string> options)
            {
                Name = name;
                Options = options.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            }
            public string Name { get; }
            public ImmutableHashSet<string> Options { get; }

            // Parses columns into their names and options. The format is: "Column | option1 option2"
            public static Column Parse(string value)
            {
                var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2) throw new ArgumentException("Column expression can contain only one pipe '|'.");
                return new Column(
                    name: parts[0].Trim(),
                    options: parts.ElementAtOrDefault(1)?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0]
                );
            }

            public static class Option
            {
                public static readonly string Key = nameof(Key);
                public static readonly string FirstLine = nameof(FirstLine);
                public static readonly string Min = nameof(Min);
                public static readonly string Max = nameof(Max);
                public static readonly string Count = nameof(Count);
                public static readonly string Sum = nameof(Sum);
                public static readonly string Avg = nameof(Avg);
                public static readonly string First = nameof(First);
                public static readonly string Last = nameof(Last);
            }
        }
    }

    public class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>>
    {
        public bool Equals(IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y)
        {
            return x.All(item => y.TryGetValue(item.Key, out TValue value) && item.Value.Equals(value));
        }

        public int GetHashCode(IDictionary<TKey, TValue> obj) => 0;
    }
}
