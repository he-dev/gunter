using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;

namespace Gunter.Messaging.Emails.Internal.Factories
{
    [ModuleFactoryFor(typeof(ITabular))]
    internal class TableFactory : ModuleFactory
    {
        private static readonly string DateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern;

        public override object Create(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            return new
            {
                Heading = format(module.Heading),
                Text = format(module.Text),
                Table = CreateTable((ITabular)module, context),
                module.Ordinal
            };
        }

        private object CreateTable(ITabular tabular, TestContext context)
        {
            using (var dataTable = tabular.Create(context))
            {
                return new
                {
                    Head = dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList(),
                    Body = 
                        dataTable
                            .AsEnumerable()
                            .Take(tabular.HasFoot ? dataTable.Rows.Count - 1 : dataTable.Rows.Count)
                            .Select(row => row.ItemArray.ToList())
                            .ToList(),
                    Foot = 
                        tabular.HasFoot
                            ? dataTable
                                .AsEnumerable()
                                .LastOrDefault()
                                ?.ItemArray
                                .ToList()
                            : null
                };                               
            }
        }
    }
}