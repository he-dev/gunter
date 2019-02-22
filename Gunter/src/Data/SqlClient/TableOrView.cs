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
    public class TableOrView : IDataSource
    {
        public TableOrView(ILogger<TableOrView> logger)
        {
            Logger = logger;
        }

        private ILogger Logger { get; }

        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [NotNull]
        [Mergeable(Required = true)]
        public string ConnectionString { get; set; }

        [NotNull]
        [Mergeable(Required = true)]
        public string Query { get; set; }

        [CanBeNull]
        [Mergeable]
        public IList<IAttachment> Attachments { get; set; }

        public async Task<(DataTable Data, string Query)> GetDataAsync(string path, RuntimeVariableDictionary runtimeVariableDictionary)
        {
            try
            {
                using (Logger.BeginScope().WithCorrelationHandle("DataSource").AttachElapsed())
                using (var conn = new SqlConnection(ConnectionString.Format(runtimeVariableDictionary) ?? throw DynamicException.Create("ConnectionStringNull", "You need to specify a connection-string.")))
                {
                    Logger.Log(Abstraction.Layer.Database().Meta(new { conn.ConnectionString }));
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = GetQuery(path, runtimeVariableDictionary);
                        cmd.CommandType = CommandType.Text;

                        Logger.Log(Abstraction.Layer.Database().Meta(new { CommandText = "See: [Message]" }), cmd.CommandText);

                        using (var dataReader = await cmd.ExecuteReaderAsync())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(dataReader);

                            EvaluateAttachments(dataTable);

                            Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = dataTable.Rows.Count, ColumnCount = dataTable.Columns.Count }));
                            Logger.Log(Abstraction.Layer.Database().Routine(nameof(GetDataAsync)).Completed());

                            return (dataTable, cmd.CommandText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw DynamicException.Create(nameof(TableOrView), $"Error getting or processing data for data-source '{Id}'.", ex);
            }
        }

        private void EvaluateAttachments(DataTable dataTable)
        {
            foreach (var attachment in Attachments ?? Enumerable.Empty<IAttachment>())
            {
                if (dataTable.Columns.Contains(attachment.Name))
                {
                    throw DynamicException.Create("ColumnAlreadyExists", $"The data-table already contains a column with the name '{attachment.Name}'.");
                }

                dataTable.Columns.Add(new DataColumn(attachment.Name, typeof(string)));

                foreach (var dataRow in dataTable.AsEnumerable())
                {
                    try
                    {
                        var value = attachment.Compute(dataRow);
                        dataRow[attachment.Name] = value;
                    }
                    catch (Exception inner)
                    {
                        throw DynamicException.Create("AttachmentCompute", $"Could not compute the '{attachment.Name}' attachment.", inner);
                    }
                }
            }
        }

        [NotNull]
        private string GetQuery(string path, RuntimeVariableDictionary runtimeVariableDictionary)
        {
            var query = Query.Format(runtimeVariableDictionary) ?? throw DynamicException.Create("QueryNull", "You need to specify a connection-string.");

            if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
            {
                var isAbsolutePath =
                    uri.AbsolutePath.StartsWith("/") == false &&
                    Path.IsPathRooted(uri.AbsolutePath);

                query =
                    isAbsolutePath
                        ? File.ReadAllText(uri.AbsolutePath)
                        : File.ReadAllText(Path.Combine(path, uri.AbsolutePath.TrimStart('/')));

                return query.Format(runtimeVariableDictionary);
            }
            else
            {
                return query;
            }
        }
    }
}