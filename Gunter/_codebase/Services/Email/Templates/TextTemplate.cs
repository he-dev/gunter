using Gunter.Data;
using Gunter.Data.Sections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace Gunter.Services.Email.Templates
{
    internal class TextTemplate : HtmlTemplate, ISectionTemplate, ISectionTemplate<TextSection>
    {
        private static class StyleName
        {
            public const string h1 = nameof(h1);
            public const string p = nameof(p);
            public const string hr = nameof(hr);
        }

        public TextTemplate() : base(new Dictionary<string, string>
        {
            [StyleName.h1] = $"font-family: Sans-Serif; color: {Theme.GreetingColor}; font-weight: normal;",
            [StyleName.p] = $"font-family: Sans-Serif; color: {Theme.MessageColor};",
            [StyleName.hr] = "border: 0; border-bottom: 1px solid #ccc; background: #ccc"
        })
        { }

        public string Render(ISection section, IConstantResolver constants) => Render((TextSection)section, constants);

        public string Render(TextSection section, IConstantResolver constants) => new StringBuilder()
            .AppendLine(string.IsNullOrEmpty(section.Heading) ? string.Empty : Html.h1(section.Heading).style(StyleName.h1).ToString())
            .AppendLine(string.IsNullOrEmpty(section.Text) ? string.Empty : Html.p(HtmlEncode(section.Text)).style(StyleName.p).ToString())
            .AppendLine(Html.hr().style(StyleName.hr).ToString())
            .ToString();

    }
}