using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.OmniLog;
using Reusable.OmniLog.SemLog;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    public class TableOrView : IDataSource
    {
        public TableOrView(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(nameof(TableOrView));
        }

        private ILogger Logger { get; }

        public int Id { get; set; }

        [NotNull]
        [JsonRequired]
        public string ConnectionString { get; set; }

        [NotNull, ItemNotNull]
        [JsonRequired]
        public List<Command> Commands { get; set; }

        public Task<DataTable> GetDataAsync(IRuntimeFormatter formatter)
        {
            if (!Commands.Any())
            {
                throw new InvalidOperationException($"You need to specify at least the one command.");
            }

            var format = (FormatFunc)formatter.Format;

            Logger.State(Layer.Database, Snapshot.Properties<TableOrView>(new { ConnectionString = format(ConnectionString) }));

            var scope = Logger.BeginScope(log => log.Elapsed());

            try
            {
                using (var conn = new SqlConnection(formatter.Format(ConnectionString)))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = format(Commands.First().Text);
                        cmd.CommandType = CommandType.Text;

                        Logger.State(Layer.Database, () => (nameof(SqlCommand.CommandText), cmd.CommandText));

                        var parameters = Commands.First().Parameters.Select(p => (Key: p.Key, Value: format(p.Value)));

                        foreach (var parameter in parameters)
                        {
                            Logger.State(Layer.Database, () => ("CommandParameter", parameter));
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }

                        using (var dataReader = cmd.ExecuteReader())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(dataReader);

                            Logger.State(Layer.Database, () => ("RowCount", dataTable.Rows.Count));
                            Logger.Success(Layer.Database);

                            return Task.FromResult(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Failure(Layer.Database, ex);
                return null;
            }
            finally
            {
                scope.Dispose();
            }
        }

        public IEnumerable<(string Name, string Text)> ToString(IRuntimeFormatter formatter)
        {
            var format = (FormatFunc)formatter.Format;

            return Commands.Select(cmd => (cmd.Name, Text: format(cmd.Text)));
        }

        //public string ToString(string format, IFormatProvider formatProvider)
        //{
        //    switch (format)
        //    {
        //        case string s when s.Equals(CommandName.Main, StringComparison.OrdinalIgnoreCase): return Commands[CommandName.Main].Text;
        //        case string s when s.Equals(CommandName.Debug, StringComparison.OrdinalIgnoreCase): return Commands[CommandName.Debug].Text;
        //        default: return base.ToString();
        //    }
        //}

    }

    //[JsonObject]
    [PublicAPI]
    public class Command
    {
        [CanBeNull]
        public string Name { get; set; }

        [NotNull]
        [JsonRequired]
        public string Text { get; set; }

        [NotNull]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}
