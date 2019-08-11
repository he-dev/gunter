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
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Data
{
    [UsedImplicitly]
    [PublicAPI]
    public interface ILog : IMergeable
    {
        [CanBeNull, ItemNotNull]
        IList<IDataFilter> Filters { get; set; }

        [ItemNotNull]
        Task<LogView> GetDataAsync(RuntimePropertyProvider runtimeProperties);
    }

    [Gunter]
    public abstract class Log : ILog
    {
        protected Log(ILogger logger) => Logger = logger;

        protected ILogger Logger { get; }

        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IList<IDataFilter> Filters { get; set; }

        public async Task<LogView> GetDataAsync(RuntimePropertyProvider runtimeProperties)
        {
            using (Logger.UseScope(correlationHandle: nameof(Log)))
            using (Logger.UseStopwatch())
            {
                Logger.Log(Abstraction.Layer.Service().Meta(new { DataSourceId = Id.ToString() }));
                try
                {
                    var result = await GetDataAsyncInternal(runtimeProperties);
                    result.GetDataElapsed = Logger.Stopwatch().Elapsed;

                    using (Logger.UseStopwatch())
                    {
                        foreach (var dataFilter in Filters ?? Enumerable.Empty<IDataFilter>())
                        {
                            dataFilter.Execute(result.Data);
                        }

                        result.FilterDataElapsed = Logger.Stopwatch().Elapsed;
                    }

                    return result;
                }
                catch (Exception inner)
                {
                    throw DynamicException.Create(GetType().ToPrettyString(), $"Error getting or processing data for data-source '{Id}'.", inner);
                }
            }
        }

        protected abstract Task<LogView> GetDataAsyncInternal(RuntimePropertyProvider runtimeProperties);
    }

    public class LogView : IDisposable
    {
        [NotNull]
        public string Query { get; set; }
        
        [CanBeNull]
        public DataTable Data { get; set; }

        public TimeSpan GetDataElapsed { get; set; }

        public TimeSpan FilterDataElapsed { get; set; }

        public void Dispose() => Data?.Dispose();
    }
}