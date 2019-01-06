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
using Gunter.Data.Attachements.Abstractions;
using Gunter.Extensions;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
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

        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [NotNull]
        [Mergable(Required = true)]
        public string ConnectionString { get; set; }

        [NotNull]
        [Mergable(Required = true)]
        public string Query { get; set; }

        [CanBeNull]
        [Mergable]
        public IList<IAttachment> Attachments { get; set; }

        public async Task<(DataTable Data, string Query)> GetDataAsync(string path, IRuntimeFormatter formatter)
        {
            Debug.Assert(!(formatter is null));

            var connectionString = ConnectionString.FormatWith(formatter);
            var query = GetQuery(path, formatter);

            var scope = Logger.BeginScope().AttachElapsed();
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

                            EvaluateAttachments(dataTable);

                            Logger.Log(Abstraction.Layer.Database().Meta(new { DataTable = new { RowCount = dataTable.Rows.Count, ColumnCount = dataTable.Columns.Count } }));
                            Logger.Log(Abstraction.Layer.Database().Routine(nameof(GetDataAsync)).Completed());

                            return (dataTable, query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw DynamicException.Create("DataSource", $"Unable to get data for {Id}.", ex);
            }
            finally
            {
                scope.Dispose();
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
                        throw DynamicException.Create("AttachementCompute", $"Could not compute the '{attachment.Name}' attachement.", inner);
                    }
                }
            }
        }

        [NotNull]
        private string GetQuery(string path, IRuntimeFormatter formatter)
        {
            var query = Query.FormatWith(formatter);

            if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
            {
                var isAbsolutePath =
                    uri.AbsolutePath.StartsWith("/") == false &&
                    Path.IsPathRooted(uri.AbsolutePath);

                query =
                    isAbsolutePath
                        ? File.ReadAllText(uri.AbsolutePath)
                        : File.ReadAllText(Path.Combine(path, uri.AbsolutePath.TrimStart('/')));

                return query.FormatWith(formatter);
            }
            else
            {
                return query;
            }
        }
    }
}