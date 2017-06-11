using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using JetBrains.Annotations;

namespace Gunter.Data
{
    [PublicAPI]
    public interface IDataSource : IResolvable, IDisposable
    {
        [JsonRequired]
        int Id { get; set; }

        [NotNull]
        [JsonIgnore]
        DataTable Data { get; }

        [NotNull]
        IEnumerable<(string Name, string Text)> GetCommands();
    }
}
