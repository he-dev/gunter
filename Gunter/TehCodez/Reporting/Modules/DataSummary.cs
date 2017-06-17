using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable.Data;

namespace Gunter.Reporting.Modules
{
    [PublicAPI]
    public class DataSummary : Module, ITabular
    {
        private delegate double AggregateCallback(IEnumerable<DataRow> aggregate, string columnName);

        private static readonly Dictionary<string, AggregateCallback> Aggregates = new Dictionary<string, AggregateCallback>(StringComparer.OrdinalIgnoreCase)
        {
            [Column.Option.Min] = (rows, column) => rows.NotNull(column).Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Min, double.NaN),
            [Column.Option.Max] = (rows, column) => rows.NotNull(column).Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Max, double.NaN),
            [Column.Option.Count] = (rows, column) => rows.NotNull(column).Count(),
            [Column.Option.Sum] = (rows, column) => rows.NotNull(column).ToDouble(column).Sum(),
            [Column.Option.Avg] = (rows, column) => rows.NotNull(column).Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Average, double.NaN),
        };

        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFooter => true;

        [NotNull]
        [ItemNotNull]
        public List<string> Columns { get; set; } = new List<string>();

        public DataTable Create(TestUnit testUnit)
        {
            // Use default columns if none-specified.
            var columns =
                (Columns.Any()
                    ? Columns.Select(Column.Parse).ToList()
                    : testUnit.DataSource.Data.Columns.Cast<DataColumn>().Select(c => new Column(c.ColumnName)))
                // Add the GroupCount column anyway.
                .Concat(new[] { Column.GroupCount }).ToList();

            // Get only keyed columns.
            var keyColumns = columns.Where(x => x.Options.Contains(Column.Option.Key)).ToList();

            // Group-by keyed columns.
            var rowGroups = testUnit.DataSource.Data.Select(testUnit.Test.Filter).GroupBy(x =>
                keyColumns.ToDictionary(
                    column => column.Name,
                    // Get field's first line if it's a string and this option is set or the value unchanged.
                    column =>
                        x[column.Name] is string s && column.Options.Contains(Column.Option.FirstLine)
                            ? GetFirstLine(s)
                            : x[column.Name],
                    StringComparer.OrdinalIgnoreCase
                ),
                new DictionaryComparer<string, object>()
            ).ToList();

            // Create the data-table.
            var dataTable = new DataTable(nameof(DataSummary));
            foreach (var column in columns)
            {
                dataTable.AddColumn(column.Name, c => c.DataType = typeof(string));
            }

            // Create aggregated rows and add them to the final data-table.
            var rows = rowGroups.Select(rowGroup => CreateRow(dataTable, columns, rowGroup));
            foreach (var row in rows)
            {
                dataTable.Rows.Add(row);
            }

            dataTable.AddRow(columns.Select(column =>
            {
                var options = string.Join(", ", column.Options);
                return (object)(string.IsNullOrEmpty(options) ? Column.Option.First : options).ToLower();
            })
            .ToArray());

            return dataTable;
        }

        private static DataRow CreateRow(DataTable dataTable, IEnumerable<Column> columns, IGrouping<IDictionary<string, object>, DataRow> group)
        {
            var dataRow = dataTable.NewRow();

            foreach (var column in columns.Where(c => !c.Equals(Column.GroupCount)))
            {
                // Try to get an aggregate function.
                var aggregate = Aggregates.FirstOrDefault(x => column.Options.Contains(x.Key));
                dataRow[column.Name] =
                    aggregate.Key == null
                        // If there is no aggregate function then either get the key value for this column or the first row from the group.
                        ? group.Key.TryGetValue(column.Name, out var value) ? value : group.First().Field<object>(column.Name)
                        // Calculate the aggregate.
                        : aggregate.Value(group, column.Name);
            }

            dataRow[Column.GroupCount.Name] = group.Count();
            return dataRow;
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
        [DebuggerDisplay("")]
        private class Column : IEquatable<Column>
        {
            // https://regex101.com/r/6UiDaq/2
            private static readonly Regex ColumnMatcher = new Regex(
                @"(?<name>\[[a-z0-9_\s]+\]|[a-z0-9_]+)(?:\s+as\s+(?<alias>\[[a-z0-9_\s]+\]|[a-z0-9_]+))?",
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            public Column(string name) : this(name, Enumerable.Empty<string>()) { }

            private Column(string name, IEnumerable<string> options)
            {
                Name = name;
                //Alias = alias;
                Options = options.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            }

            public static readonly Column GroupCount = new Column("GroupCount");

            private string DebuggerDisplay => $"Name = {Name}";

            [NotNull]
            public string Name { get; }

            //public string Alias { get; }

            //public string AliasOrName => string.IsNullOrEmpty(Alias) ? Name : Alias;

            public ImmutableHashSet<string> Options { get; }

            // Parses columns into their names and options. The format is: "Column | option1 option2"
            public static Column Parse(string value)
            {
                const int columnIndex = 0;
                const int optionsIndex = 1;

                var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2)
                {
                    throw new ArgumentException("Column expression can contain only one pipe '|'.");
                }

                //var match = ColumnMatcher.Match(parts[columnIndex]);
                //if (!match.Success)
                //{
                //    throw new ArgumentException("Invalid column name. Columns must be defined as a name or [name1 name2].");
                //}

                //var name = match.Groups["name"].Value;
                //var alias = match.Groups["alias"].Value;

                return new Column(
                    name: parts[columnIndex].Trim(),
                    options: parts.ElementAtOrDefault(optionsIndex)?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0]
                );
            }

            public bool Equals(Column other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Column)obj);
            }

            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
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

        // It doesn't make sense to calc the hash-code for the entire dictionary. Just compare the keys and values.
        public int GetHashCode(IDictionary<TKey, TValue> obj) => 0;
    }

    internal static class DataRowExtensions
    {
        public static IEnumerable<object> NotNull(this IEnumerable<DataRow> dataRows, string columnName)
        {
            return 
                dataRows
                    .Where(dataRow => dataRow[columnName] != DBNull.Value)
                    .Select(dataRow => dataRow[columnName]);
        }

        public static IEnumerable<double> ToDouble(this IEnumerable<object> values, string columnName)
        {
            return values.Select(Convert.ToDouble);
        }
    }

    internal static class DoubleExtensions
    {
        /// <summary>
        /// Aggregates throw if the collection is empty. Make sure it isn't before calculating.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="aggregate"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double AggregateOrDefault(this IEnumerable<double> values, Func<IEnumerable<double>, double> aggregate, double defaultValue)
        {
            // Min throws if the collection is empty.
            //values = values.ToList();
            return values.Any() ? aggregate(values) : defaultValue;
        }
    }
}
