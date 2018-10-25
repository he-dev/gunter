using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;

namespace Gunter.Messaging.Emails.Internal.Factories
{
    [ModuleFactoryFor(typeof(Reporting.Modules.Greeting))]
    internal class GreetingFactory : ModuleFactory
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