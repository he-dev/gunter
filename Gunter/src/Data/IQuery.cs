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
    public interface IQuery : IModel
    {
        List<IDataFilter>? Filters { get; }
    }

    [Gunter]
    public abstract class Query<T> : IQuery
    {
        [JsonRequired]
        public SoftString Name { get; set; }

        public List<IDataFilter>? Filters { get; set; } = new List<IDataFilter>?();
    }

    public interface IGetDataFrom
    {
        Type QueryType { get; }

        Task<GetDataResult> ExecuteAsync(IQuery query);
    }

    public class GetDataResult : IDisposable
    {
        public string Command { get; set; }

        public DataTable? Data { get; set; }

        public void Deconstruct(out string command, out DataTable? data) => (command, data) = (Command, Data);

        public void Dispose() => Data?.Dispose();
    }
}