using System;
using System.Collections.Generic;
using System.Globalization;
using Gunter.Data;
using Gunter.Reporting;

namespace Gunter.Messaging.Emails.ModuleFactories
{
    [ModuleFactoryFor(typeof(Reporting.Modules.Signature))]
    public class SignatureFactory : ModuleFactory
    {
        public override object Create(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            return new
            {
                Text = format($"{{{RuntimeVariableHelper.Program.FullName.Name.ToString()}}}"),
                module.Ordinal
            };
        }
    }
}