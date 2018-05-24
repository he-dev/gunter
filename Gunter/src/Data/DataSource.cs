using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Gunter.Data
{
    public interface IMergable
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonProperty("Merge")]
        string Merge { get; set; }

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
