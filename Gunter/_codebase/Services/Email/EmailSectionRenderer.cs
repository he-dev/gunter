using Reusable.Markup;
using Reusable.Markup.Extensions;
using Reusable.Markup.Renderers;
using System.Collections.Generic;
using System.Globalization;

namespace Gunter.Services.Email
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

        protected static string HtmlEncode(object value) => System.Web.HttpUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture, "{0}", value));
    }
}