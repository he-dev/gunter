using Gunter.Data.SqlClient;
using Gunter.Services;
using Reusable.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Gunter.Data.Sections
{
    internal class DataSourceSummary : ISectionFactory
    {
        public ISection Create(TestContext context, IConstantResolver constants)
        {
            var data = DataTableFactory.Create("Data source", new[] { "Property", "Value" });

            var timestampColumn = constants.Resolve(Globals.Columns.Timestamp);

            data.AddRow("Query (Main)", GetMainQuery(context.DataSource));
            data.AddRow("Query (Debug)", GetDebugQuery(context.DataSource));
            data.AddRow("Results", context.Data.Rows.Count);
            data.AddRow("CreatedOn", context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));
            data.AddRow("TimeSpan",
                context.Data.AsEnumerable().Max(r => r.Field<DateTime>(timestampColumn)) -
                context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));
            return new Section
            {
                Heading = "Data source",
                Data = data,
                Orientation = Orientation.Horizontal
            };
        }

        // TODO those two are not the prettiest but no idea how to do it better.

        private static string GetMainQuery(IDataSource dataSource)
        {
            switch (dataSource)
            {
                case TableOrViewDataSource x: return x.Commands[TableOrViewDataSource.CommandName.Main].Text;
                default: return null;
            }
        }

        private static string GetDebugQuery(IDataSource dataSource)
        {
            switch (dataSource)
            {
                case TableOrViewDataSource x: return x.Commands[TableOrViewDataSource.CommandName.Debug].Text;
                default: return null;
            }
        }
    }

}
