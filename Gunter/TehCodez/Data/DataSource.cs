using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Gunter.Data
{
    [UsedImplicitly, PublicAPI]
    public interface IDataSource
    {
        [JsonRequired]
        int Id { get; set; }

        [CanBeNull]
        Task<DataTable> GetDataAsync(IRuntimeFormatter formatter);

        IEnumerable<(string Name, string Text)> ToString(IRuntimeFormatter formatter);
    }
}
