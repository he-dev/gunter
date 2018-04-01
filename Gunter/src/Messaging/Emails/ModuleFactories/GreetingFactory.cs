using System.Collections.Generic;
using Gunter.Data;
using Gunter.Reporting;
using Reusable.Extensions;

namespace Gunter.Messaging.Emails.ModuleFactories
{
    [ModuleFactoryFor(typeof(Reporting.Modules.Greeting))]
    public class GreetingFactory : ModuleFactory
    {
        public override object Create(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            return new
            {
                Heading = format(module.Heading),
                Text = format(module.Text),
                module.Ordinal
            };            
        }
    }
}