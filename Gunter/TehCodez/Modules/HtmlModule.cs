using System;
using System.IO;
using Autofac;
using Gunter.Messaging.Emails;
using Gunter.Messaging.Emails.Renderers;
using Reusable.MarkupBuilder.Html;
using Reusable.SmartConfig;

namespace Gunter.Modules
{
    internal class HtmlModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Level>()
                .As<IRenderer>();

            builder
                .RegisterType<Greeting>()
                .As<IRenderer>();

            builder
                .RegisterType<Table>()
                .As<IRenderer>();

            builder
                .RegisterType<Signature>()
                .As<IRenderer>();

            builder
                .RegisterType<CssInliner>()
                .As<ICssInliner>();

            builder
                .RegisterType<CssParser>()
                .As<ICssParser>();            

            builder
                .RegisterType<HtmlEmail>();
        }
    }
}