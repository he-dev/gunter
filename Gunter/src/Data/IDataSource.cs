using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Services;
using JetBrains.Annotations;

namespace Gunter.Data
{
    [UsedImplicitly, PublicAPI]
    public interface IDataSource : IMergeable
    {
        [ItemNotNull]
        Task<(DataTable Data, string Query)> GetDataAsync(string path, RuntimeVariableDictionary runtimeVariableDictionary);
    }
}
