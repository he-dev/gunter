using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Gunter.Annotations;
using Reusable.Extensions;

namespace Gunter.Services.FilterData
{
    using Gunter.Services.Abstractions;

    public class ValidateService
    {
        public void Execute(object service)
        {
            Validator.ValidateObject(service, new ValidationContext(service), true);
        }
    }

    [Gunter]
    public class GetFirstLine : FilterDataBase
    {
        public override void Execute(DataTable dataTable, DataRow currentRow)
        {
            dataTable.InitializeColumn(Into);

            if (currentRow.Field<object>(From) is string value)
            {
                currentRow[Into] = value.SplitByLineBreaks().NonNullOrWhitespace().FirstOrDefault();
            }
        }
    }

    public static class DataTableHelper
    {
        public static DataTable InitializeColumn(this DataTable dataTable, string column)
        {
            if (!dataTable.Columns.Contains(column))
            {
                dataTable.Columns.Add(new DataColumn(column, typeof(object)));
            }

            return dataTable;
        }
    }
}