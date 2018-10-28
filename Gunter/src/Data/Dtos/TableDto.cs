using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;
using Reusable.Extensions;
using Reusable.Reflection;

namespace Gunter.Data.Dtos
{
    public class TripleTableDto
    {
        public TripleTableDto(IEnumerable<ColumnDto> columns, bool areHeaders = true)
        {
            //var columnList = columns.ToList();

            Head = new TableDto(columns);
            if (areHeaders)
            {
                var row = Head.NewRow();
                foreach (var column in columns)
                {
                    row[column.Name] = column.Name.ToString();
                }
            }

            Body = new TableDto(columns);
            Foot = new TableDto(columns);
        }

        //public TripleTableDto(IEnumerable<string> columns, bool areHeaders = true)
        //    : this(columns.Select(SoftString.Create), areHeaders)
        //{
        //}

        [NotNull]
        public TableDto Head { get; }

        [NotNull]
        public TableDto Body { get; }

        [NotNull]
        public TableDto Foot { get; }

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

    public class TableDto
    {
        private readonly IDictionary<SoftString, ColumnDto> _columnByName;
        private readonly IDictionary<int, ColumnDto> _columnByOrdinal;

        private readonly List<RowDto> _rows = new List<RowDto>();

        public TableDto(IEnumerable<ColumnDto> columns)
        {
            //var columns = names.Select((name, ordinal) => new ColumnDto { Name = name, Ordinal = ordinal }).ToList();
            //var columnList = columns.ToList();
            columns = columns.Select((column, ordinal) => new ColumnDto
            {
                Name = column.Name,
                Ordinal = ordinal,
                Type = column.Type
            }).ToList();
            _columnByName = columns.ToDictionary(x => x.Name);
            _columnByOrdinal = columns.ToDictionary(x => x.Ordinal);
        }

        //public TableDto(params ColumnDto[] columns) : this((IEnumerable<ColumnDto>)columns)
        //{
        //}

        internal IEnumerable<ColumnDto> Columns => _columnByName.Values;

        //[NotNull]
        //public RowDto LastRow => _rows.LastOrDefault() ?? throw new InvalidOperationException("There are no rows.");

        [NotNull]
        public RowDto NewRow()
        {
            var newRow = new RowDto
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
        public IEnumerable<IEnumerable<object>> Dump() => _rows.Select(row => row.Dump());
    }

    public static class TableDtoExtensions
    {
        public static void Add(this TableDto table, IEnumerable<object> values)
        {
            var newRow = table.NewRow();
            foreach (var (column, value) in table.Columns.Zip(values, (column, value) => (column, value)))
            {
                newRow[column.Name] = value;
            }
        }

        public static void Add(this TableDto table, params object[] values) => table.Add((IEnumerable<object>)values);
    }

    public class ColumnDto
    {
        public static readonly IComparer<ColumnDto> Comparer = ComparerFactory<ColumnDto>.Create
        (
            isLessThan: (x, y) => x.Ordinal < y.Ordinal,
            areEqual: (x, y) => x.Ordinal == y.Ordinal,
            isGreaterThan: (x, y) => x.Ordinal > y.Ordinal
        );

        public SoftString Name { get; internal set; }

        public int Ordinal { get; internal set; }

        public Type Type { get; internal set; }

        internal static ColumnDto Create<T>(SoftString name) => new ColumnDto
        {
            Name = name,
            //Ordinal = ordinal,
            Type = typeof(T)
        };

        public override string ToString() => $"{Name}[{Ordinal}]";
    }

    //public class ColumnDtoBuilder
    //{
    //    private readonly List<ColumnDto> _columns = new List<ColumnDto>();

    //    public ColumnDtoBuilder Add<T>(SoftString name)
    //    {
    //        _columns.Add(ColumnDto.Create<T>(name, _columns.Count));
    //        return this;
    //    }

    //    public static implicit operator List<ColumnDto>(ColumnDtoBuilder builder) => builder._columns;
    //}

    public class RowDto
    {
        private readonly IDictionary<ColumnDto, object> _data;
        private readonly Func<SoftString, ColumnDto> _getColumnByName;
        private readonly Func<int, ColumnDto> _getColumnByOrdinal;

        internal RowDto(IEnumerable<ColumnDto> columns, Func<SoftString, ColumnDto> getColumnByName, Func<int, ColumnDto> getColumnByOrdinal)
        {
            // All rows need to have the same length so initialize them with 'default' values.
            _data = new SortedDictionary<ColumnDto, object>(columns.ToDictionary(x => x, _ => default(object)), ColumnDto.Comparer);
            _getColumnByName = getColumnByName;
            _getColumnByOrdinal = getColumnByOrdinal;
        }

        [CanBeNull]
        public object this[SoftString name]
        {
            get => _data.GetItemSafely(_getColumnByName(name));
            set => SetValue(_getColumnByName(name), value);
        }

        [CanBeNull]
        public object this[int ordinal]
        {
            get => _data.GetItemSafely(_getColumnByOrdinal(ordinal));
            set => SetValue(_getColumnByOrdinal(ordinal), value);
        }

        private void SetValue(ColumnDto column, object value)
        {
            if (!(value is null) && value.GetType() != column.Type)
            {
                throw DynamicException.Create(
                    $"{column.Name.ToString()}Type",
                    $"The specified value has an invalid type for this column. Expected '{column.Type.Name}' but found '{value.GetType().Name}'."
                );
            }

            _data[column] = value;
        }

        [NotNull, ItemCanBeNull]
        public IEnumerable<object> Dump() => _data.Values;
    }

    public static class RowDtoExtensions
    {
        public static T Value<T>(this RowDto row, SoftString name) => row[name] is T value ? value : default;

        public static T Value<T>(this RowDto row, int ordinal) => row[ordinal] is T value ? value : default;
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