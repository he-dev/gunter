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
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Quickey;
using Reusable.Reflection;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    [UsedImplicitly]
    public class TableOrView : Log
    {
        private readonly IResourceProvider _resources;

        public TableOrView
        (
            ILogger<TableOrView> logger,
            IResourceProvider resources
        ) : base(logger)
        {
            _resources = resources;
        }

        [NotNull]
        [Mergeable(Required = true)]
        public string ConnectionString { get; set; }

        [NotNull]
        [Mergeable(Required = true)]
        public string Query { get; set; }

        protected override async Task<LogView> GetDataAsyncInternal(RuntimePropertyProvider runtimeProperties)
        {
            if (ConnectionString is null) throw new InvalidOperationException($"{nameof(TableOrView)} #{Id.ToString()} requires a connection-string.");

            var query = await GetQueryAsync(runtimeProperties);

            using (var conn = new SqlConnection(ConnectionString.Format(runtimeProperties)))
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

                        return new LogView
                        {
                            Query = cmd.CommandText,
                            Data = dataTable,
                        };
                    }
                }
            }
        }

        private async Task<string> GetQueryAsync(RuntimePropertyProvider runtimeProperties)
        {
            // language=regexp
            const string fileSchemePattern = "^file:///";
            var query = Query.Format(runtimeProperties);
            if (Regex.IsMatch(query, fileSchemePattern))
            {
                var path = Regex.Replace(query, fileSchemePattern, string.Empty);
                if (!Path.IsPathRooted(path))
                {
                    var defaultTestsDirectoryName = await _resources.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
                    path = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName, path);
                }

                query = (await _resources.ReadTextFileAsync(path)).Format(runtimeProperties);
            }

            Logger.Log(Abstraction.Layer.Database().Meta(new { CommandText = "See [Message]" }), query);

            return query;
        }
    }
}