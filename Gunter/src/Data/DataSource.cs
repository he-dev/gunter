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

        string Merge { get; set; }

        IMergable New();
    }

    [UsedImplicitly, PublicAPI]
    public interface IDataSource : IMergable
    {
        [CanBeNull]
        Task<DataTable> GetDataAsync(IRuntimeFormatter formatter);

        IEnumerable<(string Name, string Text)> ToString(IRuntimeFormatter formatter);
    }
}
