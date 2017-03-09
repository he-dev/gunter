using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Reusable;
using Reusable.Logging;
using Gunter.Services;

namespace Gunter.Data.SqlClient
{
    public class TableOrViewDataSource : DataSource
    {
        public TableOrViewDataSource(ILogger logger) : base(logger) { }

        public string ConnectionString { get; set; }

        public Dictionary<string, Command> Commands { get; set; } = new Dictionary<string, Command>();

        protected override DataTable GetDataCore(IConstantResolver constants)
        {
            using (var conn = new SqlConnection(constants.Resolve(ConnectionString)))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = constants.Resolve(Commands[CommandName.Main].Text);
                    cmd.CommandType = CommandType.Text;

                    foreach (var parameter in Commands[CommandName.Main].Parameters)
                    {
                        cmd.Parameters.AddWithValue(parameter.Key, constants.Resolve(parameter.Value));
                    }

                    using (var dataReader = cmd.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(dataReader);
                        return dataTable;
                    }
                }
            }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            if (format.Equals(CommandName.Main, StringComparison.OrdinalIgnoreCase)) return Commands[CommandName.Main].Text;
            if (format.Equals(CommandName.Debug, StringComparison.OrdinalIgnoreCase)) return Commands[CommandName.Debug].Text;

            return base.ToString();
        }

        public static class CommandName
        {
            public const string Main = nameof(Main);
            public const string Debug = nameof(Debug);
        }
    }

    public class Command
    {
        public string Text { get; set; }

        public Dictionary<string, string> Parameters { get; set; }
    }
}
