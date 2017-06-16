using System;
using System.Collections.Generic;
using System.Data;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email
{
    public interface IHtmlTemplate
    {
        string Render(TestUnit context, ISection section, IMarkupVisitor styleVisitor);
    }

    public abstract class HtmlTemplate : IHtmlTemplate
    {
        protected HtmlTemplate(IDictionary<string, string> styles)
        {
            Styles = new Dictionary<string, string>(styles, StringComparer.OrdinalIgnoreCase);
        }

        public abstract string Render(TestUnit context, ISection section, IMarkupVisitor styleVisitor);

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

}