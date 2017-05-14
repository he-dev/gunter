using System;
using Gunter.Data;
using Reusable.Markup;
using System.Collections.Generic;
using System.Globalization;

namespace Gunter.Services.Email
{
    internal abstract class HtmlTemplate
    {
        protected HtmlTemplate(IDictionary<string, string> styles)
        {
            Styles = new Dictionary<string, string>(styles, StringComparer.OrdinalIgnoreCase);
        }

        public static class Theme
        {
            public static readonly string GreetingColor = "#578761";
            public static readonly string MessageColor = "#F25F5C";
            public static readonly string SectionHeadingColor = "#7F8B7A";
            public static readonly string TableHeaderBackgroundColor = "#C6DABF";
            public static readonly string TableFooterBackgroundColor = "#D5E4D0";
        }

        protected IMarkupElement Html => MarkupElement.Builder;

        protected Dictionary<string, string> Styles { get; }
    }

    internal interface ISectionTemplate
    {
        string Render(ISection section, IConstantResolver constants);
    }

    internal interface ISectionTemplate<in T> where T : ISection
    {
        string Render(T section, IConstantResolver constants);
    }
}