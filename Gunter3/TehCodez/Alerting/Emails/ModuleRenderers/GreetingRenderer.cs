using System;
using System.Collections.Generic;
using System.Text;
using Gunter.Data;
using Gunter.Extensions;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Gunter.Services;
using Reusable.Extensions;
using Reusable.MarkupBuilder;
using Reusable.MarkupBuilder.Html;

namespace Gunter.Alerting.Emails.ModuleRenderers
{
    [CanRender(typeof(Greeting))]
    public class GreetingRenderer : ModuleRenderer
    {
        public override IEnumerable<IHtmlElement> Render(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            if (module.Heading.IsNotNullOrEmpty())
            {
                yield return
                    Html
                        .Element("h1", module.Heading)
                        .@class("module-heading");
            }

            if (module.Text.IsNotNullOrEmpty())
            {
                // This stupid " " space is required because Outlook sucks and won't render padding or margin.

                yield return
                    Html
                        .Element("p", p => p
                            .Element("span", span => span
                                .@class("test-case-severity", $"severity-{context.TestCase.Severity.ToString().ToLower()}")
                                .Append($"» {context.TestCase.Severity.ToString().ToUpper()} »"))
                            .@class("text")
                            .Append($" {context.TestCase.Message}"));
            }
        }
    }
}