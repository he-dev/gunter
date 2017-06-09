using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace Gunter.Data
{
    public interface IDataSource : IFormattable
    {
        int Id { get; }

        DataTable GetData(IConstantResolver constants);
    }

    public static class CommandName
    {
        public const string Main = nameof(Main);
        public const string Debug = nameof(Debug);
    }
}
