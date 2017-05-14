using Gunter.Data;
using Gunter.Data.Sections;
using Reusable.Markup;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Reusable.Markup.Html;

namespace Gunter.Services.Email.Templates
{
    internal class TableTemplate : HtmlTemplate, ISectionTemplate, ISectionTemplate<TableSection>
    {
        private static readonly string DateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern;

        public TableTemplate() : base(new Dictionary<string, string>
        {
            [Style.h2] = $"font-family: Sans-Serif; color: {Theme.SectionHeadingColor}; font-weight: normal;",
            [Style.table] = "border: 1px solid #742846; border-collapse: collapse; font-family: Consolas, monospace, trebuchet ms, sans-serif;",
            [Style.thead] = $"background-color: {Theme.TableHeaderBackgroundColor}; color: #303030;",
            [Style.thead_td] = "border: 1px solid #999999; padding: 5px;",
            [Style.tbody_td_property] = $"border: 1px solid #999999; padding: 5px; background-color: {Theme.TableHeaderBackgroundColor};",
            [Style.tbody_td_value] = "border: 1px solid #999999; padding: 5px;",
            [Style.tfoot] = $"font-style: italic; background-color: {Theme.TableFooterBackgroundColor}; color: #50514F; font-size: 0.75em"
        })
        { }

        public string Render(ISection section, IConstantResolver constants) => Render((TableSection)section, constants);

        public string Render(TableSection section, IConstantResolver constants) =>
            new StringBuilder()
                .AppendLine(string.IsNullOrEmpty(section.Heading) ? string.Empty : RenderHeading(section.Heading))
                .AppendLine(RenderDetailTable(section.Body, section.Footer, section.Orientation))
                .ToString();

        private string RenderHeading(string text) => Html.Element("h2", text).Style(Styles[Style.h2]).ToHtml();

        private string RenderDetailTable(DataTable data, DataTable footer, Orientation orientation)
        {
            var table = Html.Element("table").Style(Styles[Style.table]);

            if (orientation == Orientation.Horizontal)
            {
                table
                    .Element("thead", thead => thead
                        .Element("tr", tr => tr
                            .Elements("td", data.Columns.Cast<DataColumn>(), (td, x) => td
                                .Append(x.ColumnName)
                                .Style(Styles[Style.thead_td])))
                        .Style(Styles[Style.thead]));
            }

            table
                .Element("tbody", tbody => tbody
                    .Elements("tr", data.AsEnumerable(), (tr, row) => tr
                        .Elements("td", data.Columns.Cast<DataColumn>(), (td, x, i) => td
                            .Append(row.Field<string>(x.ColumnName))
                            .Style(
                                orientation == Orientation.Vertical && i == 0 
                                    ? Styles[Style.tbody_td_property] 
                                    : Styles[Style.tbody_td_value]))));

            if (footer != null)
            {
                table
                    .Element("tfoot", tfoot => tfoot
                        .Elements("tr", footer.AsEnumerable(), (tr, row) => tr
                            .Elements("td", data.Columns.Cast<DataColumn>(), (td, x) => td
                                .Append(row.Field<string>(x.ColumnName))
                                .Style(Styles[Style.tbody_td_value])))
                        //.Style(Styles[Style.tfoot])
                        );
            }

            return table.ToHtml();
        }

        private static class Style
        {
            public const string h2 = nameof(h2);
            public const string table = nameof(table);
            public const string thead = nameof(thead);
            public const string thead_td = nameof(thead_td);
            public const string tbody_td_property = nameof(tbody_td_property);
            public const string tbody_td_value = nameof(tbody_td_value);
            public const string tfoot = nameof(tfoot);
        }
    }
}