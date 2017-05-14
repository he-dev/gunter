using System.Collections.Generic;
using System.Text;
using Gunter.Data.Sections;
using Gunter.Services;
using Reusable.Extensions;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Data.Email.Templates
{
    internal class TextTemplate : HtmlTemplate, ISectionTemplate, ISectionTemplate<TextSection>
    {
        public TextTemplate() : base(new Dictionary<string, string>
        {
            [Style.h1] = $"font-family: Sans-Serif; color: {Theme.GreetingColor}; font-weight: normal;",
            [Style.p] = $"font-family: Sans-Serif; color: {Theme.MessageColor};",
            [Style.hr] = "border: 0; border-bottom: 1px solid #ccc; background: #ccc"
        })
        { }

        public string Render(ISection section, IConstantResolver constants) => Render((TextSection)section, constants);

        public string Render(TextSection section, IConstantResolver constants) => 
            new StringBuilder()
                .AppendWhen(() => section.Heading.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("h1", section.Heading).Style(Styles[Style.h1]).ToHtml()))
                .AppendWhen(() => section.Text.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("p", section.Text).Style(Styles[Style.p]).ToHtml()))
                .AppendLine(Html.Element("hr").Style(Styles[Style.hr]).ToHtml())
                .ToString();

        private static class Style
        {
            public const string h1 = nameof(h1);
            public const string p = nameof(p);
            public const string hr = nameof(hr);
        }
    }
}