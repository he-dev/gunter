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
using Gunter.Extensions;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Exceptionizer;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Reflection;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    [UsedImplicitly]
    public class TableOrView : DataSource
    {
        public TableOrView(ILogger<TableOrView> logger) : base(logger) { }

        [NotNull]
        [Mergeable(Required = true)]
        public string ConnectionString { get; set; }

        [NotNull]
        [Mergeable(Required = true)]
        public string Query { get; set; }

        protected override async Task<(DataTable Data, string Query)> GetDataAsyncInternal(string path, RuntimeVariableDictionary runtimeVariables)
        {
            if (ConnectionString is null) throw DynamicException.Create("ConnectionStringNull", "You need to specify a connection-string.");
            using (var conn = new SqlConnection(ConnectionString.Format(runtimeVariables)))
            {
                Logger.Log(Abstraction.Layer.Database().Meta(new { conn.ConnectionString }));
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = GetQuery(path, runtimeVariables);
                    cmd.CommandType = CommandType.Text;
                    Logger.Log(Abstraction.Layer.Database().Meta(new { CommandText = "See [Message]" }), cmd.CommandText);

                    using (var dataReader = await cmd.ExecuteReaderAsync())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(dataReader);

                        Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = dataTable.Rows.Count, ColumnCount = dataTable.Columns.Count }));
                        Logger.Log(Abstraction.Layer.Database().Routine(nameof(GetDataAsync)).Completed());

                        return (dataTable, cmd.CommandText);
                    }
                }
            }
        }

        [NotNull]
        private string GetQuery(string path, RuntimeVariableDictionary runtimeVariables)
        {
            var query = Query.Format(runtimeVariables);

            if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
            {
                var isAbsolutePath =
                    uri.AbsolutePath.StartsWith("/") == false &&
                    Path.IsPathRooted(uri.AbsolutePath);

                query =
                    isAbsolutePath
                        ? File.ReadAllText(uri.AbsolutePath)
                        : File.ReadAllText(Path.Combine(path, uri.AbsolutePath.TrimStart('/')));

                return query.Format(runtimeVariables);
            }
            else
            {
                return query;
            }
        }
    }
}