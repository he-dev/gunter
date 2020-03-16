using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Services;
using Reusable.Extensions;
using Reusable.Translucent;

namespace Gunter.Queries
{
    public class GetDataTableOrView : IGetData
    {
        public GetDataTableOrView(Merge merge, Format format, IResource resource)
        {
            Merge = merge;
            Format = format;
            Resource = resource;
        }

        private Merge Merge { get; }

        public Format Format { get; }

        private IResource Resource { get; }

        public Type QueryType => typeof(ITableOrView);

        public async Task<GetDataResult> ExecuteAsync(IQuery query)
        {
            return
                query is ITableOrView tableOrView
                    ? await ExecuteAsync(tableOrView)
                    : default;
        }

        private async Task<GetDataResult> ExecuteAsync(ITableOrView view)
        {
            var commandText = await GetCommandTextAsync(view);

            using var conn = new SqlConnection(view.Resolve(x => x.ConnectionString).With(Merge));

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

        private async Task<string> GetCommandTextAsync(ITableOrView view)
        {
            // language=regexp
            const string fileSchemePattern = "^file:///";
            var commandText = view.Resolve(x => x.Command).With(Merge);
            if (Regex.IsMatch(commandText, fileSchemePattern))
            {
                var path = Regex.Replace(commandText, fileSchemePattern, string.Empty);
                if (!Path.IsPathRooted(path))
                {
                    var defaultTestsDirectoryName = await Resource.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
                    path = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName, path);
                }

                commandText = (await Resource.ReadTextFileAsync(path.Map(Format))).Map(Format);
            }

            return commandText;
        }
    }
}