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
    public delegate IDictionary<string, object> ExpandFunc(object data);

    [PublicAPI]
    public class TableOrView : IDataSource
    {
        private readonly Factory _factory;

        private static readonly IDictionary<SoftString, ExpandFunc> Expanders;

        public delegate TableOrView Factory();

        static TableOrView()
        {
            Expanders = new Dictionary<SoftString, ExpandFunc>
            {
                ["json"] = data => (data is string json ? JsonExpander.Expand(json) : throw new ArgumentException($"{nameof(JsonExpander)} requires data to be a string."))
            };
        }

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
        public List<Expandable> Expandables { get; set; }

        public Task<DataTable> GetDataAsync(IRuntimeFormatter formatter)
        {
            if (!Commands.Any())
            {
                throw new InvalidOperationException($"You need to specify at least the one command.");
            }

            var format = (FormatFunc)formatter.Format;

            Logger.Log(Abstraction.Layer.Database().Data().Property(new { ConnectionString = format(ConnectionString) }));

            var scope = Logger.BeginScope(nameof(GetDataAsync)).AttachElapsed();

            try
            {
                using (var conn = new SqlConnection(formatter.Format(ConnectionString)))
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

                        Logger.Log(Abstraction.Layer.Database().Data().Object(new { cmd = cmd.CommandText, parameters }));

                        foreach (var (key, value) in parameters)
                        {
                            cmd.Parameters.AddWithValue(key, value);
                        }

                        using (var dataReader = cmd.ExecuteReader())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(dataReader);

                            Expand(dataTable);

                            Logger.Log(Abstraction.Layer.Database().Data().Object(new { RowCount = dataTable.Rows.Count }));
                            Logger.Log(Abstraction.Layer.Database().Action().Finished(nameof(GetDataAsync)));

                            return Task.FromResult(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Abstraction.Layer.Database().Action().Failed(nameof(GetDataAsync)), ex);
                return null;
            }
            finally
            {
                scope.Dispose();
            }
        }

        private void Expand(DataTable dataTable)
        {
            foreach (var expandable in (Expandables ?? Enumerable.Empty<Expandable>()).Where(e => dataTable.Columns.Contains(e.Column)))
            {
                if (!Expanders.TryGetValue(expandable.Expander, out var expand))
                {
                    throw DynamicException.Factory.CreateDynamicException($"ExpanderNotFoundException", $"Expander {expandable.Expander.QuoteWith("'")} does not exist.", null);
                }

                foreach (var dataRow in dataTable.AsEnumerable())
                {
                    var data = dataRow.Field<string>(expandable.Column);
                    if (data is null)
                    {
                        continue;
                    }

                    var properties = expand(data).ToDictionary(x => $"{expandable.Prefix}.{x.Key}", x => x.Value);

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

        public IEnumerable<(string Name, string Text)> ToString(IRuntimeFormatter formatter)
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

        [JsonRequired]
        public string Prefix { get; set; }

        [JsonRequired]
        public string Expander { get; set; }
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
