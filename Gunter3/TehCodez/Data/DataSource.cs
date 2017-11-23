using Gunter.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using JetBrains.Annotations;

namespace Gunter.Data
{
    [PublicAPI]
    public interface IDataSource
    {
        [JsonRequired]
        int Id { get; set; }

        DataTable GetData(IRuntimeFormatter formatter);
    }
}
