using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data.SqlClient;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Data
{    
    [UsedImplicitly]
    [PublicAPI]
    public interface ILog : IMergeable
    {
        [CanBeNull, ItemNotNull]
        IList<IDataPostProcessor> Then { get; set; }

        [ItemNotNull]
        Task<GetDataResult> GetDataAsync(RuntimeVariableDictionary runtimeVariables);
    }

    [GunterAttribute]
    public abstract class Log : ILog
    {
        protected Log(ILogger logger) => Logger = logger;

        protected ILogger Logger { get; }

        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IList<IDataPostProcessor> Then { get; set; }

        public async Task<GetDataResult> GetDataAsync(RuntimeVariableDictionary runtimeVariables)
        {
            using (Logger.BeginScope().WithCorrelationHandle(nameof(Log)).AttachElapsed())
            {
                Logger.Log(Abstraction.Layer.Service().Meta(new { DataSourceId = Id.ToString() }));
                try
                {
                    var getDataStopwatch = Stopwatch.StartNew();
                    var (data, query) = await GetDataAsyncInternal(runtimeVariables);
                    var getDataElapsed = getDataStopwatch.Elapsed;
                    var elapsedPostProcessing = Stopwatch.StartNew();

                    foreach (var dataPostProcessor in Then ?? Enumerable.Empty<IDataPostProcessor>())
                    {
                        dataPostProcessor.Execute(data);
                    }

                    return new GetDataResult
                    {
                        Value = data,
                        Query = query,
                        GetDataElapsed = getDataElapsed,
                        PostProcessingElapsed = elapsedPostProcessing.Elapsed
                    };
                }
                catch (Exception inner)
                {
                    throw DynamicException.Create(GetType().ToPrettyString(), $"Error getting or processing data for data-source '{Id}'.", inner);
                }
            }
        }

        protected abstract Task<(DataTable Data, string Query)> GetDataAsyncInternal(RuntimeVariableDictionary runtimeVariables);
    }

    public class GetDataResult : IDisposable
    {
        [NotNull]
        public DataTable Value { get; set; }

        [NotNull]
        public string Query { get; set; }

        public TimeSpan GetDataElapsed { get; set; }

        public TimeSpan PostProcessingElapsed { get; set; }

        public void Dispose() => Value?.Dispose();
    }
}