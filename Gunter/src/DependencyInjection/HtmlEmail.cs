using System;
using System.IO;
using Autofac;
using Gunter.Messaging.Emails;
using Gunter.Messaging.Emails.Renderers;
using Reusable.MarkupBuilder.Html;
using Reusable.SmartConfig;

namespace Gunter.Modules
{
    internal class HtmlEmail : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<LevelFactory>()
                .As<IModuleFactory>();

            builder
                .RegisterType<Greeting>()
                .As<IModuleFactory>();

            builder
                .RegisterType<Table>()
                .As<IModuleFactory>();

            builder
                .RegisterType<Signature>()
                .As<IModuleFactory>();

            builder
                .RegisterType<CssInliner>()
                .As<ICssInliner>();

            builder
                .RegisterType<CssParser>()
                .As<ICssParser>();            

            builder
                .RegisterType<Messaging.Emails.HtmlEmail>();
        }
    }
}