using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Expanders;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    public class TableOrView : IDataSource
    {
        private readonly Factory _factory;

        public delegate TableOrView Factory();

        //[JsonConstructor]
        public TableOrView(ILogger<TableOrView> logger, Factory factory)
        {
            _factory = factory;
            Logger = logger;
        }

        private ILogger Logger { get; }

        public int Id { get; set; }

        public string Merge { get; set; }

        [NotNull]
        [Mergable]
        //[JsonRequired]
        public string ConnectionString { get; set; }

        [NotNull, ItemNotNull]
        [Mergable]
        //[JsonRequired]
        public List<Command> Commands { get; set; }

        [Mergable]
        public IList<IExpander> Expanders { get; set; }

        public async Task<DataTable> GetDataAsync(IRuntimeFormatter formatter)
        {
            Debug.Assert(!(formatter is null));

            if (!Commands.Any()) throw new InvalidOperationException($"You need to specify at least the one command.");

            var format = (FormatFunc)formatter.Format;
            var scope = Logger.BeginScope(nameof(GetDataAsync)).AttachElapsed();
            var connectionString = format(ConnectionString);

            try
            {
                Logger.Log(Abstraction.Layer.Database().Variable(new { ConnectionString = format(ConnectionString) }));

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = format(Commands.First().Text);
                        cmd.CommandType = CommandType.Text;

                        var parameters =
                            Commands
                                .First()
                                .Parameters.Select(p => (p.Key, Value: format(p.Value)))
                                .ToList();

                        Logger.Log(Abstraction.Layer.Database().Variable(new { parameters }));

                        foreach (var (key, value) in parameters)
                        {
                            cmd.Parameters.AddWithValue(key, value);
                        }

                        using (var dataReader = await cmd.ExecuteReaderAsync())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(dataReader);

                            Expand(dataTable);

                            Logger.Log(Abstraction.Layer.Database().Meta(new { DataTable = new { RowCount = dataTable.Rows.Count, ColumnCount = dataTable.Columns.Count } }));
                            Logger.Log(Abstraction.Layer.Database().Routine(nameof(GetDataAsync)).Completed());

                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw DynamicException.Factory.CreateDynamicException("DataSource", "Unable to get data.", ex);
            }
            finally
            {
                scope.Dispose();
            }
        }

        private void Expand(DataTable dataTable)
        {
            foreach (var expander in (Expanders ?? Enumerable.Empty<IExpander>()).Where(e => dataTable.Columns.Contains(e.Column)))
            {
                //if (!Gunter.Expanders.TryGetValue(expandable.Expander, out var expand))
                //{
                //    throw DynamicException.Factory.CreateDynamicException("ExpanderNotFound", $"Expander {expandable.Expander.QuoteWith("'")} does not exist.");
                //}

                foreach (var dataRow in dataTable.AsEnumerable())
                {
                    var data = dataRow.Field<string>(expander.Column);
                    if (data is null)
                    {
                        continue;
                    }

                    var properties = expander.Expand(data).ToDictionary(x => $"{expander.Column}.{x.Key}", x => x.Value);

                    foreach (var property in properties.Where(x => !(x.Value is null)))
                    {
                        if (!dataTable.Columns.Contains(property.Key))
                        {
                            dataTable.Columns.Add(new DataColumn(property.Key, property.Value.GetType()));
                        }
                        dataRow[property.Key] = property.Value;
                    }
                }
            }
        }

        public IEnumerable<(string Name, string Text)> EnumerateQueries(IRuntimeFormatter formatter)
        {
            var format = (FormatFunc)formatter.Format;

            return Commands.Select(cmd => (cmd.Name, Text: format(cmd.Text)));
        }

        public IMergable New()
        {
            var mergable = _factory();
            mergable.Id = Id;
            mergable.Merge = Merge;
            return mergable;
        }
    }

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

    public class Expandable
    {
        [JsonRequired]
        public string Column { get; set; }

        public string Prefix { get; set; }

        [JsonRequired]
        public string Expander { get; set; }

        public int Index { get; set; }
    }

    public static class DataTableExpander
    {
        public static DataTable Expand(this DataTable destination, IEnumerable<IDictionary<string, object>> source)
        {
            destination = destination ?? new DataTable();

            foreach (var mapping in source)
            {
                destination.Expand(mapping);
            }

            return destination;
        }

        public static void Expand(this DataTable destination, IDictionary<string, object> source)
        {
            destination = destination ?? new DataTable();

            var newRow = destination.NewRow();

            foreach (var property in source.Where(x => !(x.Value is null)))
            {
                if (!destination.Columns.Contains(property.Key))
                {
                    destination.Columns.Add(new DataColumn(property.Key, property.Value.GetType()));
                }
                newRow[property.Key] = property.Value;
            }
            destination.Rows.Add(newRow);
        }
    }
}
