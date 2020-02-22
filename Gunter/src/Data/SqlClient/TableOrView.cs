using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    [UsedImplicitly]
    public class TableOrView : Query
    {
        private readonly IResource _resource;

        public TableOrView
        (
            ILogger<TableOrView> logger,
            IResource resource
        ) : base(logger)
        {
            _resource = resource;
        }

        [Mergeable(Required = true)]
        public string ConnectionString { get; set; }

        [Mergeable(Required = true)]
        public string Command { get; set; }
        
        [Mergeable]
        public int Timeout { get; set; }

        protected override async Task<Snapshot> GetDataAsync(RuntimePropertyProvider runtimeProperties)
        {
            if (ConnectionString is null) throw new InvalidOperationException($"{nameof(TableOrView)} #{Id.ToString()} requires a connection-string.");

            var query = await GetQueryAsync(runtimeProperties);

            using var conn = new SqlConnection(ConnectionString.Format(runtimeProperties));
            Logger.Log(Abstraction.Layer.Database().Meta(new { conn.ConnectionString }));

            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = Timeout > 0 ? Timeout : cmd.CommandTimeout;

            using var dataReader = await cmd.ExecuteReaderAsync();
            var dataTable = new DataTable();
            dataTable.Load(dataReader);

            Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = dataTable.Rows.Count, ColumnCount = dataTable.Columns.Count }));
            Logger.Log(Abstraction.Layer.Database().Routine(nameof(ExecuteAsync)).Completed());

            return new Snapshot
            {
                Command = cmd.CommandText,
                Data = dataTable,
            };
        }

        private async Task<string> GetQueryAsync(RuntimePropertyProvider runtimeProperties)
        {
            // language=regexp
            const string fileSchemePattern = "^file:///";
            var query = Command.Format(runtimeProperties);
            if (Regex.IsMatch(query, fileSchemePattern))
            {
                var path = Regex.Replace(query, fileSchemePattern, string.Empty);
                if (!Path.IsPathRooted(path))
                {
                    var defaultTestsDirectoryName = await _resource.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
                    path = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName, path).Format(runtimeProperties);
                }

                query = (await _resource.ReadTextFileAsync(path)).Format(runtimeProperties);
            }

            return query;
        }
    }
}