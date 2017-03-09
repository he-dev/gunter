using Gunter.Data;
using Gunter.Data.Sections;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace Gunter.Alerting
{
    public interface IAlert
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        string Title { get; set; }

        [JsonRequired]
        List<ISectionFactory> Sections { get; }

        void Publish(string message, IEnumerable<ISection> sections, IConstantResolver constants);
    }

    public abstract class Alert : IAlert
    {
        protected Alert(ILogger logger)
        {
            Logger = logger;
            Sections = new List<ISectionFactory>();
        }

        protected ILogger Logger { get; private set; }

        public int Id { get; set; }

        public string Title { get; set; }

        public List<ISectionFactory> Sections { get; set; }

        public void Publish(string message, IEnumerable<ISection> sections, IConstantResolver constants)
        {
            try
            {
                PublishCore(message, sections, constants);
            }
            catch (Exception ex)
            {
                LogEntry.New().Error().Exception(ex).Message("Could not publish alert.").Log(Logger);
            }
        }

        protected abstract void PublishCore(string message, IEnumerable<ISection> sections, IConstantResolver constants);
    }
}
