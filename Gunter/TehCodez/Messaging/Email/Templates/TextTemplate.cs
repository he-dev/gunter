using System.Collections.Generic;
using System.Data;
using System.Text;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Extensions;
using Reusable.Markup;
using Reusable.Markup.Formatters;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email.Templates
{
    public class TextTemplate : HtmlTemplate
    {
        public TextTemplate() : base(new Dictionary<string, string>
        {
            [Style.h1] = $"font-family: Sans-Serif; color: {Theme.GreetingColor}; font-weight: normal; margin-top: 1.5em;",
            [Style.h2] = $"font-family: Sans-Serif; color: {Theme.SectionHeadingColor}; font-weight: normal; margin-top: 1.5em;",
            [Style.p] = $"font-family: Sans-Serif; color: {Theme.MessageColor};",
            [Style.hr] = "border: 0; border-bottom: 1px solid #ccc; background: #ccc"
        })
        { }

        //public string Render(ISection section, IConstantResolver constants) => Render((TextSection)section, constants);

        public override string Render(TestUnit context, ISection section, IMarkupVisitor styleVisitor)
        {
            return new StringBuilder()
                .AppendWhen(() => section.Heading.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("h2", section.Heading).Style(Styles[Style.h2]).ToHtml(MarkupFormatting.Empty)))
                .AppendWhen(() => section.Text.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("p", section.Text).Style(Styles[Style.p]).ToHtml(MarkupFormatting.Empty)))
                //.AppendLine(Html.Element("hr").Style(Styles[Style.hr]).ToHtml())
                .ToString();
        }

        private static class Style
        {
            public const string h1 = nameof(h1);
            public const string h2 = nameof(h2);
            public const string p = nameof(p);
            public const string hr = nameof(hr);
        }
    }
}