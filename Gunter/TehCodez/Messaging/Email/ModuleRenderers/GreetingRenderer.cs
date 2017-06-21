using System;
using System.Collections.Generic;
using System.Text;
using Gunter.Data;
using Gunter.Extensions;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Reusable.Extensions;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email.ModuleRenderers
{
    [CanRender(typeof(Greeting))]
    public class GreetingRenderer : ModuleRenderer
    {
        public override string Render(IModule module, TestUnit testUnit, IServiceProvider serviceProvider)
        {
            var cssInliner = serviceProvider.GetService<CssInliner>();
            var css = serviceProvider.GetService<Css>();

            var html = new StringBuilder();

            if (module.Heading.IsNotNullOrEmpty())
            {
                html
                    .Append(Html
                        .Element("h1", module.Heading)
                        .Class("module-heading")
                        .InlineCss(cssInliner, css)
                        .ToHtml(MarkupFormatting.Empty));
            }

            if (module.Text.IsNotNullOrEmpty())
            {
                // This stupid " " space here because Outlook sucks and won't render padding or margin.

                html
                    .Append(Html
                        .Element("p", p => p
                            .Element("span", span => span
                                .Class("test-case-severity", $"severity-{testUnit.TestCase.Severity.ToString().ToLower()}")
                                .Append($"» {testUnit.TestCase.Severity.ToString().ToUpper()} »"))
                            .Class("text")
                            .Append($" {testUnit.TestCase.Message}"))
                        .InlineCss(cssInliner, css)
                        .ToHtml(MarkupFormatting.Empty));
            }

            return html.ToString();
        }
    }
}