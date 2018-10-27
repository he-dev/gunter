using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;
using Reusable.Extensions;

namespace Gunter.Data.Dtos
{   
    public class TripleTableDto
    {
        public TripleTableDto(IEnumerable<SoftString> columns, bool areHeaders = true)
        {
            Head = new TableDto<object>(columns);
            if (areHeaders)
            {
                Head.NewRow();
                foreach (var column in columns)
                {
                    Head.LastRow[column] = column.ToString();
                }
            }

            Body = new TableDto<object>(columns);
            Foot = new TableDto<object>(columns);
        }

        public TripleTableDto(IEnumerable<string> columns, bool areHeaders = true)
            : this(columns.Select(SoftString.Create), areHeaders)
        {
        }

        [NotNull]
        public TableDto<object> Head { get; }

        [NotNull]
        public TableDto<object> Body { get; }

        [NotNull]
        public TableDto<object> Foot { get; }

        [NotNull]
        public IDictionary<string, IEnumerable<IEnumerable<object>>> Dump()
        {
            return new Dictionary<string, IEnumerable<IEnumerable<object>>>
            {
                [nameof(Head)] = Head.Dump(),
                [nameof(Body)] = Body.Dump(),
                [nameof(Foot)] = Foot.Dump(),
            };
        }
    }

    public class TableDto<T>
    {
        private readonly IDictionary<SoftString, ColumnDto> _columnByName;
        private readonly IDictionary<int, ColumnDto> _columnByOrdinal;

        private readonly List<RowDto<T>> _rows = new List<RowDto<T>>();

        public TableDto(IEnumerable<SoftString> names)
        {
            var columns = names.Select((name, ordinal) => new ColumnDto { Name = name, Ordinal = ordinal }).ToList();

            _columnByName = columns.ToDictionary(x => x.Name);
            _columnByOrdinal = columns.ToDictionary(x => x.Ordinal);
        }

        public TableDto(params string[] names) : this(names.Select(SoftString.Create))
        {
        }

        internal IEnumerable<ColumnDto> Columns => _columnByName.Values;

        [NotNull]
        public RowDto<T> LastRow => _rows.LastOrDefault() ?? throw new InvalidOperationException("There are no rows.");

        [NotNull]
        public RowDto<T> NewRow()
        {
            var newRow = new RowDto<T>
            (
                _columnByName.Values,
                name => _columnByName.GetItemSafely(name),
                ordinal => _columnByOrdinal.GetItemSafely(ordinal)
            );
            _rows.Add(newRow);
            return newRow;
        }

        //public void Add(IEnumerable<T> values)
        //{
        //    var newRow = NewRow();
        //    foreach (var (column, value) in _columnByName.Values.Zip(values, (column, value) => (column, value)))
        //    {
        //        newRow[column.Name] = value;
        //    }
        //}

        //public void Add(params T[] values) => Add((IEnumerable<T>)values);

        [NotNull, ItemNotNull]
        public IEnumerable<IEnumerable<T>> Dump() => _rows.Select(row => row.Dump());
    }

    public static class TableDtoExtensions
    {
        public static void Add<T>(this TableDto<T> table, IEnumerable<T> values)
        {
            var newRow = table.NewRow();
            foreach (var (column, value) in table.Columns.Zip(values, (column, value) => (column, value)))
            {
                newRow[column.Name] = value;
            }
        }

        public static void Add<T>(this TableDto<T> table, params T[] values) => table.Add((IEnumerable<T>)values);
    }

    internal class ColumnDto
    {
        public static readonly IComparer<ColumnDto> Comparer = ComparerFactory<ColumnDto>.Create
        (
            isLessThan: (x, y) => x.Ordinal < y.Ordinal,
            areEqual: (x, y) => x.Ordinal == y.Ordinal,
            isGreaterThan: (x, y) => x.Ordinal > y.Ordinal
        );

        public SoftString Name { get; set; }

        public int Ordinal { get; set; }

        public override string ToString() => $"{Name}[{Ordinal}]";
    }

    public class RowDto<T>
    {
        private readonly IDictionary<ColumnDto, T> _data;
        private readonly Func<SoftString, ColumnDto> _getColumnByName;
        private readonly Func<int, ColumnDto> _getColumnByOrdinal;

        internal RowDto(IEnumerable<ColumnDto> columns, Func<SoftString, ColumnDto> getColumnByName, Func<int, ColumnDto> getColumnByOrdinal)
        {
            // All rows need to have the same length so initialize them with 'default' values.
            _data = new SortedDictionary<ColumnDto, T>(columns.ToDictionary(x => x, _ => default(T)), ColumnDto.Comparer);
            _getColumnByName = getColumnByName;
            _getColumnByOrdinal = getColumnByOrdinal;
        }

        [CanBeNull]
        public T this[SoftString name]
        {
            get => _data.GetItemSafely(_getColumnByName(name));
            set => _data[_getColumnByName(name)] = value;
        }

        [CanBeNull]
        public T this[int ordinal]
        {
            get => _data.GetItemSafely(_getColumnByOrdinal(ordinal));
            set => _data[_getColumnByOrdinal(ordinal)] = value;
        }

        [NotNull, ItemCanBeNull]
        public IEnumerable<T> Dump() => _data.Values;
    }

    internal static class ComparerFactory<T>
    {
        private class Comparer : IComparer<T>
        {
            private readonly Func<T, T, bool> _isLessThan;
            private readonly Func<T, T, bool> _areEqual;
            private readonly Func<T, T, bool> _isGreaterThan;

            public Comparer(Func<T, T, bool> isLessThan, Func<T, T, bool> areEqual, Func<T, T, bool> isGreaterThan)
            {
                _isLessThan = isLessThan;
                _areEqual = areEqual;
                _isGreaterThan = isGreaterThan;
            }

            public int Compare(T x, T y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(x, null)) return -1;
                if (ReferenceEquals(y, null)) return 1;

                if (_isLessThan(x, y)) return -1;
                if (_areEqual(x, y)) return 0;
                if (_isGreaterThan(x, y)) return 1;

                // Makes the compiler very happy.
                return 0;
            }
        }

        public static IComparer<T> Create(Func<T, T, bool> isLessThan, Func<T, T, bool> areEqual, Func<T, T, bool> isGreaterThan)
        {
            return new Comparer(isLessThan, areEqual, isGreaterThan);
        }
    }
}