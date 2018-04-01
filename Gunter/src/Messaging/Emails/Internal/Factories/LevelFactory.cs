using Gunter.Data;
using Gunter.Reporting;

namespace Gunter.Messaging.Emails.Internal.Factories
{
    [ModuleFactoryFor(typeof(Reporting.Modules.Level))]
    internal class LevelFactory : ModuleFactory
    {
        public override object Create(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var level = context.TestCase.Level.ToString();

            return new
            {
                Text = level,
                module.Ordinal
            };            
        }
    }
}