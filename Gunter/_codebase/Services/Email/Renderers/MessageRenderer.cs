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
            [StyleName.h1] = "font-family: Segoe UI, Sans-Serif; color: #d2143a;",
            [StyleName.p] = "font-family: Segoe UI, Sans-Serif; color: #30303;",
            [StyleName.hr] = "border: 0; border-bottom: 1px dashed #ccc; background: #999"
        })
        { }

        public string Message { get; set; }

        public string Render(string message) => new StringBuilder()
            .AppendLine(Html.h1("Glitch alert").style(StyleName.h1).ToString())
            .AppendLine(Html.p(message).style(StyleName.p).ToString())
            .AppendLine(Html.hr().style(StyleName.hr).ToString())
            .ToString();
    }
}