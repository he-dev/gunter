using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Dynamic;
using System.Linq;

namespace Gunter.Data.Sections
{
    public class Aggregation : SectionFactory
    {
        private delegate double AggregateCallback(IEnumerable<DataRow> aggregate, string columnName);

        private static readonly Dictionary<string, AggregateCallback> Aggregates = new Dictionary<string, AggregateCallback>(StringComparer.OrdinalIgnoreCase)
        {
            [Column.Option.Min] = (rows, column) => rows.Min(row => row.Field<double>(column)),
            [Column.Option.Max] = (rows, column) => rows.Max(row => row.Field<double>(column)),
            [Column.Option.Count] = (rows, column) => rows.Count(),
            [Column.Option.Sum] = (rows, column) => rows.Sum(row => row.Field<double>(column)),
            [Column.Option.Avg] = (rows, column) => rows.Average(row => row.Field<double>(column)),
        };

        public Aggregation(ILogger logger) : base(logger) { }

        [JsonRequired]
        public List<string> Columns { get; set; }

        protected override ISection CreateCore(TestContext context, IConstantResolver constants)
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
                        return
                            column.Options.Contains("firstline", StringComparer.OrdinalIgnoreCase)
                                ? GetFirstLine(value)
                                : value;
                    },
                    StringComparer.OrdinalIgnoreCase
                ),
                new DictionaryComparer<string, string>()
            ).ToList();

            // Creates a data-table with the specified columns.
            var data = new DataTable("Aggregation");
            columns.ForEach(x => data.AddColumn(x.Name, c => c.DataType = typeof(string)));            

            // Aggregate the groups.
            foreach (var g in groups)
            {
                var newRow = data.NewRow();

                foreach (var column in columns)
                {
                    // Try to get an aggregate function.
                    var aggregate = Aggregates.FirstOrDefault(x => column.Options.Contains(x.Key));
                    newRow[column.Name] =
                        aggregate.Key == null
                            // If there is no aggregate function then either get the key value for this column or the first row from the group.
                            ? g.Key.TryGetValue(column.Name, out string value) ? value : g.First().Field<object>(column.Name)
                            // Calculate the aggregate.
                            : aggregate.Value(g, column.Name);
                }               

                data.Rows.Add(newRow);
            }

            return new Section
            {
                Heading = "Exceptions",
                Data = data,
                Orientation = Orientation.Horizontal
            };
        }



        private static string GetFirstLine(string value)
        {
            // Extracts the first line. Currently used for exception message that is always the first line of an exception string.
            return
                string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

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
