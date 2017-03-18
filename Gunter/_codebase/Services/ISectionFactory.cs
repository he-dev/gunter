using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace Gunter.Services
{
    public interface ISectionFactory
    {
        string Heading { get; set; }

        string Text { get; set; }

        ISection Create(TestContext context);
    }

    public abstract class SectionFactory : ISectionFactory
    {
        protected SectionFactory(ILogger logger) => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected ILogger Logger { get; }

        [JsonRequired]
        public string Heading { get; set; }

        public string Text { get; set; }

        public ISection Create(TestContext context)
        {
            try
            {
                return CreateCore(context);
            }
            catch (Exception ex)
            {
                LogEntry.New().Error().Exception(ex).Message("Error creating section.").Log(Logger);
                return null;
            }
        }

        protected abstract ISection CreateCore(TestContext context);
    }
}
