using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Reporting;
using Reusable.Extensions;
using Reusable.MarkupBuilder;
using Reusable.MarkupBuilder.Html;

namespace Gunter.Messaging.Emails.ModuleRenderers
{
    [CanRender(typeof(ITabular))]
    public class TableRenderer : ModuleRenderer
    {
        private static readonly string DateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern;

        public override IEnumerable<IHtmlElement> Render(IModule module, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            if (!(module is ITabular tabular))
            {
                yield break;
            }

            if (module.Heading.IsNotNullOrEmpty())
            {
                yield return Html
                    .Element("h2", h2 => h2
                        .@class("module-heading")
                        .Append(format(module.Heading)));
            }

            if (module.Text.IsNotNullOrEmpty())
            {
                yield return Html
                    .Element("p", p => p
                        .@class("text")
                        .Append(format(module.Text)));
            }

            yield return RenderTable(tabular, context);

            // .AppendWhen(() => module.Heading.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("h2", module.Heading).Class("section-heading").ToHtml(MarkupFormatting.Empty)))
            //.AppendWhen(() => module.Text.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("p", module.Text).ToHtml(MarkupFormatting.Empty)));
            //.AppendLine(Html.Element("hr").Style(Styles[Style.hr]).ToHtml())
        }

        private IHtmlElement RenderTable(ITabular tabular, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            using (var dataTable = tabular.Create(context))
            {
                var table = Html
                    .Element("table")
                    .@class("table");

                if (tabular.Orientation == TableOrientation.Horizontal)
                {
                    var tr = Html.Element("tr");

                    foreach (var dataColumn in dataTable.Columns.Cast<DataColumn>())
                    {
                        // Normally we would use "th" here but Outlook sucks and renders only crap.
                        tr
                            .Element("th", th => th
                                .@class("cell", "table-header")
                                .Append(dataColumn.ColumnName));
                    }

                    var thead = Html.Element("thead");
                    thead.Add(tr);
                    table.Add(thead);
                }

                var tbody = Html.Element("tbody");

                for (var i = 0; i < dataTable.Rows.Count; i++)
                {
                    var dataRow = dataTable.Rows[i];

                    var tr = Html.Element("tr");

                    var isLastRow = i == dataTable.Rows.Count - 1;
                    if (tabular.HasFooter && isLastRow)
                    {
                        tr.@class("table-footer");
                    }

                    foreach (var dataColumn in dataTable.Columns.Cast<DataColumn>())
                    {
                        if (dataColumn.Ordinal == 0 && tabular.Orientation == TableOrientation.Vertical)
                        {
                            // Normally we would use "th" here but Outlook sucks and renders only crap.
                            tr
                                .Element("th", th => th
                                    .@class("cell", "table-header")
                                    .Append(dataRow.Field<string>(dataColumn.ColumnName)));
                        }
                        else
                        {
                            tr
                                .Element("td", td => td
                                    .@class("cell")
                                    .Append(dataRow.Field<string>(dataColumn.ColumnName)));
                        }
                    }

                    tbody.Add(tr);
                }

                table.Add(tbody);

                return table;
            }
        }
    }
}