using Gunter.Services;
using Gunter.Testing;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace Gunter.Data.Sections
{
    public interface ISectionFactory
    {
        ISection Create(TestContext context, IConstantResolver constants);
    }

    public abstract class SectionFactory : ISectionFactory
    {
        protected SectionFactory(ILogger logger) => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected ILogger Logger { get; }

        public ISection Create(TestContext context, IConstantResolver constants)
        {
            try
            {
                return CreateCore(context, constants);
            }
            catch (Exception ex)
            {
                LogEntry.New().Error().Exception(ex).Message("Error creating section.").Log(Logger);
                return null;
            }
        }

        protected abstract ISection CreateCore(TestContext context, IConstantResolver constants);
    }
}
