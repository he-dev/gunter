using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    [UsedImplicitly, PublicAPI]
    public interface IDataSource : IMergeable
    {
        [CanBeNull, ItemNotNull]
        IList<IDataPostProcessor> Then { get; set; }

        [ItemNotNull]
        Task<GetDataResult> GetDataAsync(string path, RuntimeVariableDictionary runtimeVariables);
    }

    public abstract class DataSource : IDataSource
    {
        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IList<IDataPostProcessor> Then { get; set; }

        public async Task<GetDataResult> GetDataAsync(string path, RuntimeVariableDictionary runtimeVariables)
        {
            var data = await GetDataAsyncInternal(path, runtimeVariables);
            var elapsedPostProcessing = Stopwatch.StartNew();
            foreach (var dataPostProcessor in Then ?? Enumerable.Empty<IDataPostProcessor>())
            {
                dataPostProcessor.Execute(data.Value);
            }

            data.ElapsedPostProcessing = elapsedPostProcessing.Elapsed;
            return data;
        }

        protected abstract Task<GetDataResult> GetDataAsyncInternal(string path, RuntimeVariableDictionary runtimeVariables);
    }

    public class GetDataResult : IDisposable
    {
        [CanBeNull]
        public DataTable Value { get; set; }

        [NotNull]
        public string Query { get; set; }

        public TimeSpan ElapsedQuery { get; set; }

        public TimeSpan ElapsedPostProcessing { get; set; }

        public void Dispose() => Value?.Dispose();
    }
}