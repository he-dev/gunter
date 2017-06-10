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
        private readonly ILogger _logger;

        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();

        public TableOrViewDataSource(ILogger logger)
        {
            _logger = logger;
        }

        [JsonRequired]
        public int Id { get; }

        [JsonRequired]
        public string ConnectionString { get; set; }

        [JsonRequired]
        public Dictionary<string, Command> Commands
        {
            get => _commands;
            set
            {
                if (!value.ContainsKey(CommandName.Main))
                {
                    LogEntry.New().Fatal().Message($"{CommandName.Main} command not specified.").Log(_logger);
                    throw new ArgumentException(
                        paramName: nameof(Commands),
                        message: $"You need to specify at least the {nameof(CommandName.Main)} command.");
                }
                _commands = value;
            }
        }

        public DataTable GetData(IConstantResolver constants)
        {
            if (!Commands.TryGetValue(CommandName.Main, out var command))
            {
                LogEntry.New().Fatal().Message($"{CommandName.Main} command not specified.").Log(_logger);
                throw new InvalidOperationException($"You need to specify at least the {CommandName.Main} command.");
            }

            var connectionString = constants.Resolve(ConnectionString);
            LogEntry.New().Debug().Message($"Connection string: {connectionString}").Log(_logger);

            using (var conn = new SqlConnection(constants.Resolve(ConnectionString)))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = constants.Resolve(command.Text);
                    cmd.CommandType = CommandType.Text;

                    LogEntry.New().Debug().Message($"Command text: {cmd.CommandText}").Log(_logger);

                    foreach (var parameter in command.Parameters)
                    {
                        var parameterValue = constants.Resolve(parameter.Value);
                        LogEntry.New().Debug().Message($"Parameter: {parameter.Key} = '{parameterValue}'").Log(_logger);
                        cmd.Parameters.AddWithValue(parameter.Key, parameterValue);
                    }

                    using (var dataReader = cmd.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(dataReader);
                        LogEntry.New().Debug().Message($"Row count: {dataTable.Rows.Count}").Log(_logger);
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
