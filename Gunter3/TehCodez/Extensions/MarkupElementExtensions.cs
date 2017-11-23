using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Extensions
{
    internal static class MarkupElementExtensions
    {
        public static IMarkupElement InlineCss(this IMarkupElement element, CssInliner cssInliner, Css css)
        {
            return cssInliner.Inline(css, element);
        }
    }
}
