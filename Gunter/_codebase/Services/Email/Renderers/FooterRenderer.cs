﻿using Reusable;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gunter.Services.Email.Renderers
{
    internal class FooterRenderer : EmailSectionRenderer
    {
        public static class StyleName
        {
            public const string Author = nameof(Author);
            public const string Paragraph = nameof(Paragraph);
        }

        public FooterRenderer() : base(new Dictionary<string, string>
        {
            [StyleName.Author] = "color: #0066cc;",
            [StyleName.Paragraph] = "font-family: sans-serif; font-size: 12px; color: #A0A0A0;"
        })
        { }

        public string Render(string name, DateTime timestamp)
        {
            var p = Html.p
            (
                "Auto-generated by {Name} ({Timestamp}) (UTC)".Format(new
                {
                    Name = Html.span(HtmlEncode(name)).style(StyleName.Author),
                    Timestamp = timestamp.ToString(CultureInfo.InvariantCulture)
                })
            ).style(StyleName.Paragraph);

            var result = p.ToString();
            return result;
        }
    }
}