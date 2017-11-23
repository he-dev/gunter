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
using Reusable.OmniLog;

namespace Gunter.Data.SqlClient
{
    public class TableOrView : IDataSource
    {
        private readonly ILogger _logger;
        private readonly Lazy<DataTable> _data;

        public TableOrView(ILogger logger)
        {
            _logger = logger;
            _data = new Lazy<DataTable>(GetData);
        }

        [NotNull]
        [JsonIgnore]
        public IRuntimeFormatter Variables
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

        public bool IsFaulted { get; private set; }

        [PublicAPI]
        [NotNull]
        [JsonRequired]
        public string ConnectionString { get; set; }        

        [PublicAPI]
        [NotNull, ItemNotNull]
        [JsonRequired]
        public List<Command> Commands { get; set; }

        public DataTable GetData(IRuntimeFormatter formatter)
        {
            if (!Commands.Any())
            {
                throw new InvalidOperationException($"You need to specify at least the one command.");
            }

            //LogEntry.New().Debug().Message($"Connection string: {ConnectionString}").Log(_logger);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                using (var conn = new SqlConnection(formatter.Format(ConnectionString)))
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

                            //stopwatch.Stop();
                            //Elapsed = stopwatch.Elapsed;

                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                IsFaulted = true;
                _logger.Log(e => e.Error().Exception(ex).Message("Could not get data."));
                return null;
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

    //[JsonObject]
    public class Command
    {
        private string _text;

        [NotNull]
        [JsonIgnore]
        public IRuntimeFormatter Variables { get; set; } = RuntimeFormatter.Empty;

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
    }
}
