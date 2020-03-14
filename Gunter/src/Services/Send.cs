using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Workflows;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    public interface ISend
    {
        Task InvokeAsync(TestContext context);
    }

    public abstract class Send : ISend
    {
        protected Send(ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        public abstract Task InvokeAsync(TestContext context);
    }
}