using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Services.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Data.Abstractions
{
    [UsedImplicitly]
    [PublicAPI]
    public interface IQuery : IMergeable
    {
        List<IFilterData>? Filters { get; }
    }

    [Gunter]
    public abstract class Query : IQuery
    {
        [JsonRequired]
        public string Name { get; set; } = default!;
        
        public ModelSelector? ModelSelector { get; set; }
        
        public List<IFilterData>? Filters { get; set; } = new List<IFilterData>();
    }

    public interface IService<in T, TResult>
    {
        Task<TResult> ExecuteAsync(T parameter);
    }

    public interface IGetData<in T> where T : IQuery
    {
        Task<GetDataResult> ExecuteAsync(T query);
    }

    public class GetDataResult : IDisposable
    {
        public string Command { get; set; } = default!;
        
        public DataTable Data { get; set; } = default!;

        public void Deconstruct(out string command, out DataTable data) => (command, data) = (Command, Data);

        public void Dispose() => Data.Dispose();
    }
}