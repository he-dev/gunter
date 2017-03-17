using Gunter.Alerts;
using Gunter.Data;
using Gunter.Data.Sections;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Gunter
{
    public interface IAlert
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        string Title { get; set; }

        [JsonRequired]
        List<ISectionFactory> Sections { get; }

        void Publish(TestContext testContext, IConstantResolver constants);
    }

    public abstract class Alert : IAlert
    {
        protected Alert(ILogger logger) => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected ILogger Logger { get; private set; }

        [JsonRequired]
        public int Id { get; set; }

        [JsonRequired]
        public string Title { get; set; }

        [JsonRequired]
        public List<ISectionFactory> Sections { get; set; } = new List<ISectionFactory>();        

        public void Publish(TestContext testContext, IConstantResolver constants)
        {
            try
            {
                var sections = Sections.Select(factory => factory.Create(testContext, constants));
                PublishCore(sections, constants);
            }
            catch (Exception ex)
            {
                LogEntry.New().Error().Exception(ex).Message("Error publishing alert.").Log(Logger);
            }
        }

        protected abstract void PublishCore(IEnumerable<ISection> sections, IConstantResolver constants);
    }
}
