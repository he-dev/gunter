using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gunter.Annotations;
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
    }


    public class GetDataFromTableOrView : IGetDataFrom
    {
        public GetDataFromTableOrView(IResource resource)
        {
            Resource = resource;
        }

        private IResource Resource { get; }

        public Type QueryType => typeof(ITableOrView);

        public async Task<GetDataResult> ExecuteAsync(IQuery query, RuntimeContainer container)
        {
            return
                query is ITableOrView tableOrView
                    ? await ExecuteAsync(tableOrView, container)
                    : default;
        }

        private async Task<GetDataResult> ExecuteAsync(ITableOrView view, RuntimeContainer container)
        {
            var commandText = await GetCommandTextAsync(view, container);

            using var conn = new SqlConnection(view.ConnectionString.Format(container));

            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = view.Timeout > 0 ? view.Timeout : cmd.CommandTimeout;

            using var dataReader = await cmd.ExecuteReaderAsync();
            var dataTable = new DataTable();
            dataTable.Load(dataReader);

            return new GetDataResult
            {
                Command = cmd.CommandText,
                Data = dataTable,
            };
        }

        private async Task<string> GetCommandTextAsync(ITableOrView view, RuntimeContainer container)
        {
            // language=regexp
            const string fileSchemePattern = "^file:///";
            var commandText = view.Command.Format(container);
            if (Regex.IsMatch(commandText, fileSchemePattern))
            {
                var path = Regex.Replace(commandText, fileSchemePattern, string.Empty);
                if (!Path.IsPathRooted(path))
                {
                    var defaultTestsDirectoryName = await Resource.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
                    path = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName, path).Format(container);
                }

                commandText = (await Resource.ReadTextFileAsync(path)).Format(container);
            }

            return commandText;
        }
    }
}