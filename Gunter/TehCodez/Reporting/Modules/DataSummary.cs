using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gunter.Data;
using Gunter.Reporting.Filters;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Data;

namespace Gunter.Reporting.Modules
{
    [PublicAPI]
    public class DataSummary : Module, ITabular
    {
        private delegate object AggregateCallback(IEnumerable<DataRow> aggregate, string columnName);

        private static readonly Dictionary<ColumnTotal, AggregateCallback> Aggregates = new Dictionary<ColumnTotal, AggregateCallback>
        {
            [ColumnTotal.First] = (rows, column) => rows.Select(column).NotDBNull().FirstOrDefault(),
            [ColumnTotal.Last] = (rows, column) => rows.Select(column).NotDBNull().LastOrDefault(),
            [ColumnTotal.Min] = (rows, column) => rows.Select(column).NotDBNull().Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Min, double.NaN),
            [ColumnTotal.Max] = (rows, column) => rows.Select(column).NotDBNull().Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Max, double.NaN),
            [ColumnTotal.Count] = (rows, column) => rows.Select(column).NotDBNull().Count(),
            [ColumnTotal.Sum] = (rows, column) => rows.Select(column).NotDBNull().Select(Convert.ToDouble).Sum(),
            [ColumnTotal.Average] = (rows, column) => rows.Select(column).NotDBNull().Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Average, double.NaN),
        };

        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFooter => true;

        [DefaultValue(true)]
        public bool IncludeGroupCount { get; set; } = true;

        [NotNull]
        [ItemCanBeNull]
        public List<ColumnOption> Columns { get; set; } = new List<ColumnOption>();

        public DataTable Create(TestUnit testUnit)
        {
            var columns = Columns.ToList();

            if (!Columns.Any())
            {
                // Use default columns if none-specified.
                columns = testUnit.DataSource.Data.Columns.Cast<DataColumn>().Select(c => new ColumnOption
                {
                    Name = c.ColumnName,
                    Total = ColumnTotal.Last
                }).ToList();
            }

            if (IncludeGroupCount)
            {
                columns.Add(ColumnOption.GroupCount);
            }

            // Group-by keyed columns.
            var dataRows = testUnit.DataSource.Data.Select(testUnit.TestCase.Filter);

            var keyColumns = columns.Where(x => x.IsKey).ToList();
            var rowGroups =
                from dataRow in dataRows
                let values = keyColumns.Select(column => column.Filter.Apply(dataRow[column.Name]))
                group dataRow by new CompositeKey<object>(values) into g
                select g;

            // Create the data-table.
            var dataTable = new DataTable(nameof(DataSummary));
            foreach (var column in columns)
            {
                dataTable.AddColumn(column.Name, c => c.DataType = typeof(string));
            }

            // Create aggregated rows and add them to the final data-table.
            var rows = rowGroups.Select(rowGroup => AggregateRows(dataTable, columns, rowGroup));
            foreach (var row in rows)
            {
                dataTable.Rows.Add(row);
            }

            // Add the footer row with column options.
            IEnumerable<string> GetColumnOptions(ColumnOption column)
            {
                if (column.IsKey) yield return "Key";
                if (column.Filter != null && !(column.Filter is Unchanged)) yield return column.Filter.GetType().Name;
                yield return column.Total.ToString();
            }
            dataTable.AddRow(columns.Select(column => (object)string.Join(", ", GetColumnOptions(column))).ToArray());

            return dataTable;
        }

        private DataRow AggregateRows(DataTable dataTable, IEnumerable<ColumnOption> columns, IGrouping<CompositeKey<object>, DataRow> group)
        {
            var dataRow = dataTable.NewRow();

            foreach (var column in columns.Where(c => !c.Equals(ColumnOption.GroupCount)))
            {
                // Try to get an aggregate function.
                var aggregate = Aggregates[column.Total];

                var value = aggregate(group, column);
                dataRow[column.Name] = column.Filter == null ? value : column.Filter.Apply(value);
            }

            if (IncludeGroupCount)
            {
                dataRow[ColumnOption.GroupCount] = group.Count();
            }
            return dataRow;
        }
    }

    // Represents an aggregated column and its options.
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}"), PublicAPI]
    public class ColumnOption : IEquatable<ColumnOption>
    {
        public static readonly ColumnOption GroupCount = new ColumnOption
        {
            Name = "GroupCount",
            Total = ColumnTotal.Count
        };

        [JsonRequired]
        public string Name { get; set; }

        public bool IsKey { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDataFilter Filter { get; set; } = new Unchanged();

        public ColumnTotal Total { get; set; }

        private string DebuggerDisplay => $"Name = {Name} IsKey = {IsKey} Filter = {Filter?.GetType().Name ?? "null"} Total = {Total}";

        public bool Equals(ColumnOption other)
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
            return Equals((ColumnOption)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        }

        public static implicit operator string(ColumnOption column) => column.Name;
    }

    public enum ColumnTotal
    {
        First,
        Last,
        Min,
        Max,
        Count,
        Sum,
        Average,
    }

    public class CompositeKey<T> : IEnumerable<T>, IEquatable<CompositeKey<T>>
    {
        private readonly List<T> _keys;

        public CompositeKey(IEnumerable<T> keys) => _keys = new List<T>(keys);

        public IEnumerator<T> GetEnumerator() => _keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(CompositeKey<T> other)
        {
            if (ReferenceEquals(other, null)) return false;
            return ReferenceEquals(this, other) || this.SequenceEqual(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompositeKey<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return this.Aggregate(0, (current, next) => (current * 397) ^ next?.GetHashCode() ?? 0);
            }
        }
    }

    //public class CollectionComparer<TValue> : IEqualityComparer<IEnumerable<TValue>>
    //{
    //    public bool Equals(IEnumerable<TValue> x, IEnumerable<TValue> y)
    //    {
    //        if (ReferenceEquals(x, null)) return false;
    //        if (ReferenceEquals(y, null)) return false;
    //        return ReferenceEquals(x, y) || x.SequenceEqual(y);
    //    }

    //    public int GetHashCode(IEnumerable<TValue> obj)
    //    {
    //        unchecked
    //        {
    //            return obj.Aggregate(0, (current, next) => (current * 397) ^ next?.GetHashCode() ?? 0);
    //        }
    //    }
    //}

    internal static class DataRowExtensions
    {
        public static IEnumerable<object> Select(this IEnumerable<DataRow> dataRows, string columnName)
        {
            return dataRows.Select(dataRow => dataRow[columnName]);
        }

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object> NotDBNull(this IEnumerable<object> dataRows)
        {
            return dataRows.Where(value => value != DBNull.Value);
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
