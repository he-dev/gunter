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

        [CanBeNull]
        [JsonIgnore]
        DataTable Data { get; }

        /// <summary>
        /// Returns true if data could not be retrieved.
        /// </summary>
        [JsonIgnore]
        bool IsFaulted { get; }

        [JsonIgnore]
        TimeSpan Elapsed { get; }

        [NotNull]
        IEnumerable<(string Name, string Text)> GetCommands();
    }
}
