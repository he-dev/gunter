using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Reusable.Logging;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Logging.Loggex;

namespace Gunter.Data.SqlClient
{
    public class TableOrView : IDataSource
    {
        private readonly ILogger _logger;
        private readonly Lazy<DataTable> _data;

        private List<Command> _commands = new List<Command>();
        private string _connectionString;
        private IVariableResolver _variables = VariableResolver.Empty;

        public TableOrView(ILogger logger)
        {
            _logger = logger;
            _data = new Lazy<DataTable>(GetData);
        }

        [NotNull]
        [JsonIgnore]
        public IVariableResolver Variables
        {
            get => _variables;
            set
            {
                _variables = value;
                foreach (var command in _commands)
                {
                    command.UpdateVariables(value);
                }
            }
        }

        public int Id { get; set; }

        public DataTable Data => _data.Value;

        public TimeSpan Elapsed { get; private set; }

        [PublicAPI]
        [NotNull]
        [JsonRequired]
        public string ConnectionString
        {
            get => Variables.Resolve(_connectionString);
            set => _connectionString = value;
        }

        [PublicAPI]
        [NotNull]
        [ItemNotNull]
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

        private DataTable GetData()
        {
            if (!Commands.Any())
            {
                throw new InvalidOperationException($"You need to specify at least the one command.");
            }

            //LogEntry.New().Debug().Message($"Connection string: {ConnectionString}").Log(_logger);

            var stopwatch = Stopwatch.StartNew();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = Commands.First().Text;
                    cmd.CommandType = CommandType.Text;

                    //LogEntry.New().Debug().Message($"Command: {cmd.CommandText}").Log(_logger);

                    foreach (var parameter in Commands.First())
                    {
                        //LogEntry.New().Debug().Message($"Parameter: {parameter.Key} = '{parameter.Value}'").Log(_logger);
                        cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    using (var dataReader = cmd.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(dataReader);
                        //LogEntry.New().Debug().Message($"Row count: {dataTable.Rows.Count}").Log(_logger);

                        stopwatch.Stop();
                        Elapsed = stopwatch.Elapsed;

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

        public void Dispose()
        {
            if (_data.IsValueCreated)
            {
                _data.Value.Dispose();
            }
        }
    }

    [JsonObject]
    public class Command : IResolvable, IEnumerable<KeyValuePair<string, string>>
    {
        private string _text;

        [NotNull]
        [JsonIgnore]
        public IVariableResolver Variables { get; set; } = VariableResolver.Empty;

        [CanBeNull]
        public string Name { get; set; }

        [NotNull]
        [JsonRequired]
        public string Text
        {
            get => Variables.Resolve(_text);
            set => _text = value;
        }

        [PublicAPI]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return
                Parameters
                    .Select(parameter => new KeyValuePair<string, string>(parameter.Key, Variables.Resolve(parameter.Value)))
                    .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
