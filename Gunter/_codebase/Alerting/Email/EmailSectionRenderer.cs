using Reusable.Markup;
using Reusable.Markup.Extensions;
using Reusable.Markup.Renderers;
using System.Collections.Generic;

namespace Gunter.Alerting.Email
{
    internal abstract class EmailSectionRenderer
    {
        protected EmailSectionRenderer(Dictionary<string, string> styles)
        {
            Html = new MarkupBuilder(new HtmlRenderer());
            (Html as MarkupBuilder)
                .Extensions
                    .Add<cssExtension>()
                    .Add<attrExtension>()
                    .Add(new styleExtension(styles));
        }

        protected dynamic Html { get; }

        private static string HtmlEncode(string query) => System.Web.HttpUtility.HtmlEncode(query);
    }
}