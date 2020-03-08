using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data.Abstractions;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.Flowingo;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;

namespace Gunter.Data.SqlClient
{
    public interface ITableOrView : IQuery, IMergeable
    {
        string ConnectionString { get; }

        string Command { get; }

        int Timeout { get; }
    }

    [PublicAPI]
    [UsedImplicitly]
    public class TableOrView : Query<ITableOrView>, ITableOrView
    {
        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public string ConnectionString { get; set; }

        public string Command { get; set; }

        public int Timeout { get; set; }

        public IModel Merge(IEnumerable<TheoryFile> templates) => new TableOrViewUnion(this, templates);
    }

    public class TableOrViewUnion : Union<ITableOrView>, ITableOrView
    {
        public TableOrViewUnion(ITableOrView model, IEnumerable<TheoryFile> templates) : base(model, templates) { }

        public string ConnectionString => GetValue(x => x.ConnectionString, x => x is {});

        public string Command => GetValue(x => x.Command, x => x is {});

        public int Timeout => GetValue(x => x.Timeout, x => x > 0);

        public List<IDataFilter> Filters => GetValue(x => x.Filters, x => x?.Any() == true);

        public IModel Merge(IEnumerable<TheoryFile> templates) => new TableOrViewUnion(this, templates);
    }

    public class GetDataFromTableOrView : IGetDataFrom
    {
        public GetDataFromTableOrView(IResource resource)
        {
            Resource = resource;
        }

        private IResource Resource { get; }

        public Type SourceType => typeof(ITableOrView);

        public async Task<QueryResult> ExecuteAsync(IQuery query, RuntimePropertyProvider runtimeProperties)
        {
            return
                query is ITableOrView tableOrView
                    ? await ExecuteAsync(tableOrView, runtimeProperties)
                    : default;
        }

        private async Task<QueryResult> ExecuteAsync(ITableOrView view, RuntimePropertyProvider runtimeProperties)
        {
            var commandText = await GetCommandTextAsync(view, runtimeProperties);

            using var conn = new SqlConnection(view.ConnectionString.Format(runtimeProperties));

            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = view.Timeout > 0 ? view.Timeout : cmd.CommandTimeout;

            using var dataReader = await cmd.ExecuteReaderAsync();
            var dataTable = new DataTable();
            dataTable.Load(dataReader);

            return new QueryResult
            {
                Command = cmd.CommandText,
                Data = dataTable,
            };
        }

        private async Task<string> GetCommandTextAsync(ITableOrView view, RuntimePropertyProvider runtimeProperties)
        {
            // language=regexp
            const string fileSchemePattern = "^file:///";
            var commandText = view.Command.Format(runtimeProperties);
            if (Regex.IsMatch(commandText, fileSchemePattern))
            {
                var path = Regex.Replace(commandText, fileSchemePattern, string.Empty);
                if (!Path.IsPathRooted(path))
                {
                    var defaultTestsDirectoryName = await Resource.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
                    path = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName, path).Format(runtimeProperties);
                }

                commandText = (await Resource.ReadTextFileAsync(path)).Format(runtimeProperties);
            }

            return commandText;
        }
    }
}