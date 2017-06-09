using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Reusable;
using Reusable.Logging;
using Gunter.Services;
using Newtonsoft.Json;

namespace Gunter.Data.SqlClient
{
    public class TableOrViewDataSource : IDataSource
    {
        [JsonRequired]
        public int Id { get; }

        [JsonRequired]
        public string ConnectionString { get; set; }

        [JsonRequired]
        public Dictionary<string, Command> Commands { get; set; } = new Dictionary<string, Command>();

        public DataTable GetData(IConstantResolver constants)
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

        public string ToString(string format, IFormatProvider formatProvider)
        {
            switch (format)
            {
                case string s when s.Equals(CommandName.Main, StringComparison.OrdinalIgnoreCase): return Commands[CommandName.Main].Text;
                case string s when s.Equals(CommandName.Debug, StringComparison.OrdinalIgnoreCase): return Commands[CommandName.Debug].Text;
                default: return base.ToString();
            }
        }
    }

    public class Command
    {
        [JsonRequired]
        public string Text { get; set; }

        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}
