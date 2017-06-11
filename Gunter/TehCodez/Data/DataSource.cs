using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace Gunter.Data
{
    public interface IDataSource : IResolvable
    {
        int Id { get; }

        DataTable GetData();

        IEnumerable<(string Name, string Text)> GetCommands();
    }
}
