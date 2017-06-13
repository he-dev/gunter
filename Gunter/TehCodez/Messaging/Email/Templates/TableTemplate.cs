using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email.Templates
{
    internal class TableTemplate : HtmlTemplate
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

        public override string Render(TestUnit context, ISection section)
        {
            if (section.Detail == null)
            {
                return string.Empty;
            }

            using (var detail = section.Detail.Create(context))
            {
                return new StringBuilder()
                    //.AppendLine(string.IsNullOrEmpty(section.Heading) ? string.Empty : RenderHeading(section.Heading))
                    .AppendLine(
                        RenderDetailTable(
                            detail,
                            section.Detail.Orientation))
                    .ToString();
            }
        }

        private string RenderHeading(string text) => Html.Element("h2", text).Style(Styles[Style.h2]).ToHtml();

        private string RenderDetailTable(DataSet data, TableOrientation orientation)
        {
            var body = data.Tables[0];
            var footer = data.Tables.Count > 1 ? data.Tables[1] : null;

            var table = Html.Element("table").Style(Styles[Style.table]);

            if (orientation == TableOrientation.Horizontal)
            {
                table
                    .Element("thead", thead => thead
                        .Element("tr", tr => tr
                            .Elements("td", body.Columns.Cast<DataColumn>(), (td, x) => td
                                .Append(x.ColumnName)
                                .Style(Styles[Style.thead_td])))
                        .Style(Styles[Style.thead]));
            }

            table
                .Element("tbody", tbody => tbody
                    .Elements("tr", body.AsEnumerable(), (tr, row) => tr
                        .Elements("td", body.Columns.Cast<DataColumn>(), (td, x) => td
                            .Append(row.Field<string>(x.ColumnName))
                            .Style(
                                orientation == TableOrientation.Vertical && x.Ordinal == 0
                                    ? Styles[Style.tbody_td_property]
                                    : Styles[Style.tbody_td_value]))));

            if (footer != null)
            {
                table
                    .Element("tfoot", tfoot => tfoot
                        .Elements("tr", footer.AsEnumerable(), (tr, row) => tr
                            .Elements("td", body.Columns.Cast<DataColumn>(), (td, x) => td
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