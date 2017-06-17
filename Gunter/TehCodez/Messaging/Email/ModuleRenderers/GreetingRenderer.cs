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
            var styleVisitor = serviceProvider.GetService<StyleVisitor>();

            var html = new StringBuilder();

            if (module.Heading.IsNotNullOrEmpty())
            {
                html
                    .Append(Html
                        .Element("h1", module.Heading)
                        .Class("module-heading")
                        .ToHtml(MarkupFormatting.Empty, new[] { styleVisitor }));
            }

            if (module.Text.IsNotNullOrEmpty())
            {
                // This stupid " " space here because Outlook sucks and won't render padding or margin.

                html
                    .Append(Html
                        .Element("p", p => p
                            .Element("span", span => span
                                .Class("test-case-severity", $"severity-{testUnit.Test.Severity.ToString().ToLower()}")
                                .Append($"» {testUnit.Test.Severity.ToString().ToUpper()} »"))
                            .Class("text")
                            .Append($" {testUnit.Test.Message}"))
                        .ToHtml(MarkupFormatting.Empty, new[] { styleVisitor }));
            }

            return html.ToString();
        }
    }
}