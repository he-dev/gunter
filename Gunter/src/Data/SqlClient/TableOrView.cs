using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Expanders;
using Gunter.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Utilities;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    public class TableOrView : IDataSource
    {
        private readonly Program _program;
        private readonly Factory _factory;

        public delegate TableOrView Factory();

        //[JsonConstructor]
        public TableOrView(ILogger<TableOrView> logger, Program program, Factory factory)
        {
            _program = program;
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

        [NotNull]
        [Mergable]
        //[JsonRequired]
        public string Query { get; set; }

        [CanBeNull]
        [Mergable]
        public IList<IExpander> Expanders { get; set; }

        public async Task<DataTable> GetDataAsync(IRuntimeFormatter formatter)
        {
            Debug.Assert(!(formatter is null));

            if (Query.IsNullOrEmpty()) throw new InvalidOperationException("You need to specify the Query property.");

            var scope = Logger.BeginScope().AttachElapsed();
            var connectionString = ConnectionString.FormatWith(formatter);
            var query = ToString(formatter);

            try
            {
                Logger.Log(Abstraction.Layer.Database().Composite(new { properties = new { connectionString, query } }));

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = query;
                        cmd.CommandType = CommandType.Text;

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
            var columnExpanders = (Expanders ?? Enumerable.Empty<IExpander>()).Where(expander => dataTable.Columns.Contains(expander.Column));

            foreach (var expander in columnExpanders)
            {
                foreach (var dataRow in dataTable.AsEnumerable())
                {
                    var value = dataRow.Field<string>(expander.Column);
                    if (!(value is null))
                    {
                        var properties = expander.Expand(value).ToDictionary(x => $"{expander.Column}.{x.Key}", x => x.Value);

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
        }

        public string ToString(IRuntimeFormatter formatter)
        {
            var query = Query.FormatWith(formatter);

            try
            {
                if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
                {
                    var isAbsolutePath =
                        uri.AbsolutePath.StartsWith("/") == false &&
                        Path.IsPathRooted(uri.AbsolutePath);

                    query =
                        isAbsolutePath
                            ? File.ReadAllText(uri.AbsolutePath)
                            : File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), _program.TestsDirectoryName, uri.AbsolutePath.TrimStart('/')));

                    return query.FormatWith(formatter);
                }
                else
                {
                    return query;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(ToString)).Faulted(), ex);
                return null;
            }
        }

        public IMergable New()
        {
            var mergable = _factory();
            mergable.Id = Id;
            mergable.Merge = Merge;
            return mergable;
        }
    }    
}
