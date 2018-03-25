using System.Collections.Generic;
using Gunter.Data;
using Gunter.Reporting;
using Reusable.Extensions;
using Reusable.MarkupBuilder;
using Reusable.MarkupBuilder.Html;

namespace Gunter.Messaging.Emails.Renderers
{
    [CanRender(typeof(Reporting.Modules.Greeting))]
    public class Greeting : Renderer
    {
        public override IEnumerable<IHtmlElement> Render(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            // <h3 class="heading">Hi, everyone.</h3> 
            // <p class="paragraph message">There is something wrong with the service.</p>

            if (module.Heading.IsNotNullOrEmpty())
            {
                yield return 
                    Html
                        .Element("h3", format(module.Heading))
                        .@class("heading");
            }

            if (module.Text.IsNotNullOrEmpty())
            {
                yield return 
                    Html
                        .Element("p", p => p
                            .Append($"{format(context.TestCase.Message)}")
                            .@class("paragraph message")
                        );
            }
        }
    }
}