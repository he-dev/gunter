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

        public static class Theme
        {
            public static readonly string GreetingColor = "#578761";
            public static readonly string MessageColor = "#F25F5C";
            public static readonly string SectionHeadingColor = "#7F8B7A";
            public static readonly string TableHeaderBackgroundColor = "#C6DABF";
            public static readonly string TableFooterBackgroundColor = "#D5E4D0";
        }
    }
}