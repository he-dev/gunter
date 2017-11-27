using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.OmniLog;

namespace Gunter
{
    [UsedImplicitly]
    public class RuntimeFormatterFactory : IRuntimeFormatterFactory
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IRuntimeVariable> _runtimeVariables;

        public RuntimeFormatterFactory(ILoggerFactory loggerFactory, IEnumerable<IRuntimeVariable> runtimeVariables)
        {
            _logger = loggerFactory.CreateLogger<RuntimeFormatterFactory>();
            _runtimeVariables = runtimeVariables.ToList();
        }

        public IRuntimeFormatter Create(IDictionary<SoftString, object> locals, params object[] args)
        {
            var runtimeVariables = args.Select(_runtimeVariables.Resolve).SelectMany(x => x);                

            // todo add state log

            return new RuntimeFormatter(locals.Concat(runtimeVariables));
        }       
    }
}