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

    public abstract class DataSource : IDataSource
    {
        protected DataSource(ILogger logger) => Logger = logger;

        [JsonRequired]
        public int Id { get; set; }

        protected ILogger Logger { get; private set; }

        public DataTable GetData(IConstantResolver constants)
        {
            try
            {
                return GetDataCore(constants);
            }
            catch (Exception ex)
            {
                LogEntry.New().Error().Exception(ex).Message("Could not get data.").Log(Logger);
                return new DataTable();
            }
        }

        protected abstract DataTable GetDataCore(IConstantResolver constants);

        public abstract string ToString(string format, IFormatProvider formatProvider);
    }
}
