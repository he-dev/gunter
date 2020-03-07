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
    public interface IQuery : IPartial
    {
        [ItemNotNull]
        IList<IDataFilter>? Filters { get; set; }

        [ItemNotNull]
        Task<QueryResult> ExecuteAsync(RuntimePropertyProvider runtimeProperties);
    }

    [Gunter]
    public abstract class Query : IQuery
    {
        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IList<IDataFilter>? Filters { get; set; }

        public abstract Task<QueryResult> ExecuteAsync(RuntimePropertyProvider runtimeProperties);
    }

    public class QueryResult : IDisposable
    {
        public string Command { get; set; }

        public DataTable? Data { get; set; }

        public void Dispose() => Data?.Dispose();
    }
}