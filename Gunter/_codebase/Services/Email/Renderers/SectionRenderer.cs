using Gunter.Data;
using Gunter.Testing;
using Reusable.Markup;
using Reusable.Markup.Extensions;
using Reusable.Markup.Renderers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Gunter.Services.Email.Renderers
{
    internal class SectionRenderer : EmailSectionRenderer
    {
        private static readonly string DateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern;

        private static class StyleName
        {
            public const string h2 = nameof(h2);
            public const string table = nameof(table);
            public const string thead = nameof(thead);
            public const string thead_td = nameof(thead_td);
            public const string tbody_td_property = nameof(tbody_td_property);
            public const string tbody_td_value = nameof(tbody_td_value);
            public const string tfoot = nameof(tfoot);
        }

        public SectionRenderer() : base(new Dictionary<string, string>
        {
            [StyleName.h2] = $"font-family: Sans-Serif; color: {Theme.SectionHeadingColor}; font-weight: normal;",
            [StyleName.table] = "border: 1px solid #742846; border-collapse: collapse; font-family: Consolas, monospace, trebuchet ms, sans-serif;",
            [StyleName.thead] = $"background-color: {Theme.TableHeaderBackgroundColor}; color: #303030;",
            [StyleName.thead_td] = "border: 1px solid #999999; padding: 5px;",
            [StyleName.tbody_td_property] = $"border: 1px solid #999999; padding: 5px; background-color: {Theme.TableHeaderBackgroundColor};",
            [StyleName.tbody_td_value] = "border: 1px solid #999999; padding: 5px;",
            [StyleName.tfoot] = $"font-style: italic; background-color: {Theme.TableFooterBackgroundColor}; color: #50514F; font-size: 0.75em"
        })
        { }

        public string Render(ISection section) => new StringBuilder()
            .AppendLine(RenderHeading(section.Title))
            .AppendLine(RenderDetailTable(section.Data, section.Footer, section.Orientation))
            .ToString();

        private string RenderHeading(string text) => Html.h2(HtmlEncode(text)).style(StyleName.h2).ToString();

        private string RenderDetailTable(DataTable data, DataTable footer, Orientation orientation)
        {
            var table = Html.table().style(StyleName.table);

            if (orientation == Orientation.Horizontal)
            {
                table.thead
                (
                    Html.tr(data.Columns.Cast<DataColumn>().Select(x => Html.td(HtmlEncode(x.ColumnName)).style(StyleName.thead_td)))
                )
                .style(StyleName.thead);
            }

            table.tbody
            (
                data.AsEnumerable().Select(row =>
                    Html.tr(
                        data.Columns.Cast<DataColumn>().Select((c, i) =>
                            Html.td(HtmlEncode(row.Field<string>(c.ColumnName))).style(
                            orientation == Orientation.Vertical && i == 0
                                ? StyleName.tbody_td_property
                                : StyleName.tbody_td_value
                            )
                        )
                    )
                )
            );

            if (footer != null)
            {
                table.tfoot
                (
                    footer.AsEnumerable().Select(row =>
                        Html.tr(
                            data.Columns.Cast<DataColumn>().Select((c, i) =>
                                Html.td(HtmlEncode(row.Field<string>(c.ColumnName))).style(StyleName.tbody_td_value)
                            )
                        )
                    )
                )
                .style(StyleName.tfoot);
            }

            return table.ToString();
        }
    }
}