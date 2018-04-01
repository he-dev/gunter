using Gunter.Data;
using Gunter.Reporting;

namespace Gunter.Messaging.Emails.Internal.Factories
{
    [ModuleFactoryFor(typeof(Reporting.Modules.Signature))]
    internal class SignatureFactory : ModuleFactory
    {
        public override object Create(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            return new
            {
                Text = format($"{RuntimeVariable.Program.FullName}"),
                module.Ordinal
            };
        }
    }
}