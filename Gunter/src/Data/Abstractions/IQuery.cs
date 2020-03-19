using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Services.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Data.Abstractions
{
    [UsedImplicitly]
    [PublicAPI]
    public interface IQuery : IModel
    {
        List<IFilterData>? Filters { get; }
    }

    [Gunter]
    public abstract class Query : IQuery
    {
        [JsonRequired]
        public string? Name { get; set; }

        public List<IFilterData>? Filters { get; set; } = new List<IFilterData>();
    }

    public interface IGetData
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