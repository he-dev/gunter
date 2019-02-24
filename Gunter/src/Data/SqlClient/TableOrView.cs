using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Extensions;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Exceptionizer;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Reflection;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    [UsedImplicitly]
    public class TableOrView : Log
    {
        private readonly ProgramInfo _programInfo;
        private readonly IResourceProvider _resources;

        public TableOrView
        (
            ILogger<TableOrView> logger,
            ProgramInfo programInfo,
            IResourceProvider resources
        ) : base(logger)
        {
            _programInfo = programInfo;
            _resources = resources;
        }

        [NotNull]
        [Mergeable(Required = true)]
        public string ConnectionString { get; set; }

        [NotNull]
        [Mergeable(Required = true)]
        public string Query { get; set; }

        protected override async Task<(DataTable Data, string Query)> GetDataAsyncInternal(RuntimeVariableDictionary runtimeVariables)
        {
            if (ConnectionString is null) throw DynamicException.Create("ConnectionStringNull", "You need to specify a connection-string.");

            var query = await GetQueryAsync(runtimeVariables);

            using (var conn = new SqlConnection(ConnectionString.Format(runtimeVariables)))
            {
                Logger.Log(Abstraction.Layer.Database().Meta(new { conn.ConnectionString }));
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;

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

        private async Task<string> GetQueryAsync(RuntimeVariableDictionary runtimeVariables)
        {
            // language=regexp
            const string fileSchemePattern = "^file:///";
            var query = Query.Format(runtimeVariables);
            if (Regex.IsMatch(query, fileSchemePattern))
            {
                var path = Regex.Replace(query, fileSchemePattern, string.Empty);
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(_programInfo.CurrentDirectory, _programInfo.DefaultTestsDirectoryName, path);
                }

                query = (await _resources.ReadTextFileAsync(path)).Format(runtimeVariables);
            }

            Logger.Log(Abstraction.Layer.Database().Meta(new { CommandText = "See [Message]" }), query);

            return query;
        }
    }
}