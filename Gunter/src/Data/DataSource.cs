using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Services;
using JetBrains.Annotations;

namespace Gunter.Data
{
    public interface IIdentifiable
    {
        [JsonRequired]
        int Id { get; set; }
    }

    public interface IMergable : IIdentifiable
    {
        //[JsonProperty("Merge")]
        Merge Merge { get; set; }
    }

    [UsedImplicitly, PublicAPI]
    public interface IDataSource : IMergable
    {
        [ItemNotNull]
        Task<DataTable> GetDataAsync(IRuntimeFormatter formatter);

        [CanBeNull]
        string ToString(IRuntimeFormatter formatter);
    }
}
