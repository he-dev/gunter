using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
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
    public interface IQuery : IMergeable
    {
        [ItemNotNull]
        IList<IDataFilter>? Filters { get; set; }

        [ItemNotNull]
        Task<Snapshot> ExecuteAsync(RuntimePropertyProvider runtimeProperties);
    }

    [Gunter]
    public abstract class Query : IQuery
    {
        protected Query(ILogger logger) => Logger = logger;

        protected ILogger Logger { get; }

        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IList<IDataFilter>? Filters { get; set; }

        public async Task<Snapshot> ExecuteAsync(RuntimePropertyProvider runtimeProperties)
        {
            using (Logger.BeginScope().WithCorrelationHandle("ExecuteQuery").UseStopwatch())
            {
                Logger.Log(Abstraction.Layer.Service().Subject(new { DataSourceId = Id.ToString() }));
                try
                {
                    var snapshot = await GetDataAsync(runtimeProperties);
                    snapshot.GetDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                    
                    return ApplyFilters(snapshot);
                }
                catch (Exception inner)
                {
                    throw DynamicException.Create(GetType().ToPrettyString(), $"Error getting or processing data for data-source '{Id}'.", inner);
                }
            }
        }

        private Snapshot ApplyFilters(Snapshot snapshot)
        {
            if (Filters is {} filters)
            {
                using (Logger.BeginScope().WithCorrelationHandle("ExecuteFilters").UseStopwatch())
                {
                    foreach (var dataFilter in filters)
                    {
                        dataFilter.Execute(snapshot.Data);
                    }

                    snapshot.FilterDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                }
            }

            return snapshot;
        }

        protected abstract Task<Snapshot> GetDataAsync(RuntimePropertyProvider runtimeProperties);
    }

    public class Snapshot : IDisposable
    {
        public string Command { get; set; }
        
        public DataTable? Data { get; set; }

        public TimeSpan GetDataElapsed { get; set; }

        public TimeSpan FilterDataElapsed { get; set; }

        public void Dispose() => Data?.Dispose();
    }
}