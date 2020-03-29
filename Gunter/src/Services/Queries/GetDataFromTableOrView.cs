using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Queries;
using Gunter.Helpers;
using Gunter.Services.Abstractions;
using Reusable.Extensions;
using Reusable.Translucent;

namespace Gunter.Services.Queries
{
    public class GetDataFromTableOrView : IGetData<TableOrView>
    {
        public GetDataFromTableOrView(IMergeScalar mergeScalar, ITryGetFormatValue tryGetFormatValue, IResource resource)
        {
            MergeScalar = mergeScalar;
            TryGetFormatValue = tryGetFormatValue;
            Resource = resource;
        }

        private IMergeScalar MergeScalar { get; }

        private ITryGetFormatValue TryGetFormatValue { get; }

        private IResource Resource { get; }

        public async Task<GetDataResult> ExecuteAsync(TableOrView view)
        {
            var commandText = await GetCommandTextAsync(view);

            using var conn = new SqlConnection(view.Resolve(x => x.ConnectionString, MergeScalar).Format(TryGetFormatValue));

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

        private async Task<string> GetCommandTextAsync(TableOrView view)
        {
            // language=regexp
            const string fileSchemePattern = "^file:///";

            var commandText = view.Resolve(x => x.Command, MergeScalar).Format(TryGetFormatValue);

            if (Regex.Replace(commandText, fileSchemePattern, string.Empty) is var path && path.Length < commandText.Length)
            {
                if (!Path.IsPathRooted(path))
                {
                    var defaultTestsDirectoryName = await Resource.ReadSettingAsync(ProgramConfig.DefaultTestsDirectoryName);
                    path = Path.Combine(ProgramInfo.CurrentDirectory, defaultTestsDirectoryName, path);
                }

                commandText = (await Resource.ReadTextFileAsync(path.Format(TryGetFormatValue)));
            }


            return commandText.Format(TryGetFormatValue);
        }
    }
}