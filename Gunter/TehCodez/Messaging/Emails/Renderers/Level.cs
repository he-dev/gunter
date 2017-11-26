using System.Collections.Generic;
using Gunter.Data;
using Gunter.Reporting;
using Reusable.MarkupBuilder;
using Reusable.MarkupBuilder.Html;

namespace Gunter.Messaging.Emails.Renderers
{
    [CanRender(typeof(Reporting.Modules.Level))]
    public class Level : Renderer
    {
        public override IEnumerable<IHtmlElement> Render(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            // <p class="test-case-level trace">Trace</p>

            var level = context.TestCase.Level.ToString();

            yield return 
                Html
                    .Element("p", p => p
                        .Append(level)
                        .@class($"test-case-level {level.ToLower()}")
                    );
        }
    }
}