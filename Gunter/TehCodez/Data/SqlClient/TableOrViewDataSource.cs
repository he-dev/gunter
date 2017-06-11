using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Reusable;
using Reusable.Logging;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Data;

namespace Gunter.Data.SqlClient
{
    public class TableOrViewDataSource : IDataSource
    {
        private readonly ILogger _logger;

        private List<Command> _commands = new List<Command>();
        private string _connectionString;
        private IConstantResolver _constants = ConstantResolver.Empty;

        public TableOrViewDataSource(ILogger logger)
        {
            _logger = logger;
        }

        [JsonIgnore]
        public IConstantResolver Constants
        {
            get => _constants;
            set
            {
                _constants = value;
                foreach (var command in _commands)
                {
                    command.UpdateConstants(value);
                }
            }
        }

        [JsonRequired]
        public int Id { get; }

        [JsonRequired]
        public string ConnectionString
        {
            get => Constants.Resolve(_connectionString);
            set => _connectionString = value;
        }

        [JsonRequired]
        public List<Command> Commands
        {
            get => _commands;
            set
            {
                if (!value.Any())
                {
                    throw new ArgumentException(
                        paramName: nameof(Commands),
                        message: $"You need to specify at least the one command.");
                }
                _commands = value;
            }
        }

        public DataTable GetData()
        {
            if (!Commands.Any())
            {
                throw new InvalidOperationException($"You need to specify at least the one command.");
            }

            LogEntry.New().Debug().Message($"Connection string: {ConnectionString}").Log(_logger);

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = Commands.First().Text;
                    cmd.CommandType = CommandType.Text;

                    LogEntry.New().Debug().Message($"Command: {cmd.CommandText}").Log(_logger);

                    foreach (var parameter in Commands.First())
                    {
                        LogEntry.New().Debug().Message($"Parameter: {parameter.Key} = '{parameter.Value}'").Log(_logger);
                        cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
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

        public IEnumerable<(string Name, string Text)> GetCommands()
        {
            return
                from command in Commands
                select (command.Name, command.Text);
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

    public class Command : IResolvable, IEnumerable<KeyValuePair<string, string>>
    {
        private string _text;
        public IConstantResolver Constants { get; set; }

        public string Name { get; set; }

        [JsonRequired]
        public string Text
        {
            get => Constants.Resolve(_text);
            set => _text = value;
        }

        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return 
                Parameters
                    .Select(parameter => new KeyValuePair<string, string>(parameter.Key, Constants.Resolve(parameter.Value)))
                    .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
