using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Gunter.Services;
using JetBrains.Annotations;

namespace Gunter.Data
{
    public interface IMergable
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonProperty("Merge")]
        Merge Merge { get; set; }

        IMergable New();
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
