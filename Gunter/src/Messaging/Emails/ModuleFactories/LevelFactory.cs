using System.Collections.Generic;
using Gunter.Data;
using Gunter.Reporting;

namespace Gunter.Messaging.Emails.Renderers
{
    [ModuleFactoryFor(typeof(Reporting.Modules.Level))]
    public class LevelFactory : ModuleFactory
    {
        public override object Create(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var level = context.TestCase.Level.ToString();

            return new
            {
                Text = level
            };            
        }
    }
}