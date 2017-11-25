using System;
using System.IO;
using Autofac;
using Gunter.Alerting.Emails;
using Gunter.Alerting.Emails.ModuleRenderers;
using Reusable.MarkupBuilder.Html;
using Reusable.SmartConfig;

namespace Gunter.Modules
{
    internal class HtmlModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<GreetingRenderer>()
                .As<ModuleRenderer>();

            builder
                .RegisterType<TableRenderer>()
                .As<ModuleRenderer>();

            builder
                .RegisterType<SignatureRenderer>()
                .As<ModuleRenderer>();

            builder
                .RegisterType<CssInliner>();
            //.As<ICssInliner>();

            builder
                .RegisterType<CssParser>()
                .As<ICssParser>();            

            builder
                .RegisterType<HtmlEmail>();
        }
    }
}