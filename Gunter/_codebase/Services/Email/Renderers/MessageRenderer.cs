using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace Gunter.Services.Email.Renderers
{
    internal class MessageRenderer : EmailSectionRenderer
    {
        private static class StyleName
        {
            public const string h1 = nameof(h1);
            public const string p = nameof(p);
            public const string hr = nameof(hr);
        }

        public MessageRenderer() : base(new Dictionary<string, string>
        {
            [StyleName.h1] = "font-family: Sans-Serif; color: #50514F; font-weight: normal;",
            [StyleName.p] = "font-family: Sans-Serif; color: #F25F5C;",
            [StyleName.hr] = "border: 0; border-bottom: 1px solid #ccc; background: #CCC"
        })
        { }

        public string Message { get; set; }

        public string Render(string message) => new StringBuilder()
            .AppendLine(Html.h1("Glitch alert").style(StyleName.h1).ToString())
            .AppendLine(Html.p(HtmlEncode(message)).style(StyleName.p).ToString())
            .AppendLine(Html.hr().style(StyleName.hr).ToString())
            .ToString();
    }
}