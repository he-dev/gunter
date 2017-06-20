using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Gunter.Data;
using Gunter.Extensions;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Extensions;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email.ModuleRenderers
{
    [CanRender(typeof(ITabular))]
    public class TableRenderer : ModuleRenderer
    {
        private static readonly string DateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern;

        public override string Render(IModule module, TestUnit testUnit, IServiceProvider serviceProvider)
        {
            if (module is ITabular tabular)
            {
                var cssInliner = serviceProvider.GetService<CssInliner>();
                var css = serviceProvider.GetService<Css>();
                var variables = serviceProvider.GetService<IVariableResolver>();


                var heading = string.IsNullOrEmpty(module.Heading) ? null : Html
                    .Element("h2", h2 => h2
                        .Class("module-heading")
                        .Append(module.Heading))
                    .InlineCss(cssInliner, css)
                    .ToHtml(MarkupFormatting.Empty);

                var text = string.IsNullOrEmpty(module.Text) ? null : Html
                    .Element("p", p => p
                        .Class("text")
                        .Append(module.Heading))
                    .InlineCss(cssInliner, css)
                    .ToHtml(MarkupFormatting.Empty);

                var table = RenderTable(tabular, testUnit, serviceProvider);

                return new StringBuilder()
                    .AppendLine(heading)
                    .AppendLine(text)
                    .AppendLine(table)
                    .ToString();

                //html
                // .AppendWhen(() => module.Heading.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("h2", module.Heading).Class("section-heading").ToHtml(MarkupFormatting.Empty)))
                //.AppendWhen(() => module.Text.IsNotNullOrEmpty(), sb => sb.AppendLine(Html.Element("p", module.Text).ToHtml(MarkupFormatting.Empty)));
                //.AppendLine(Html.Element("hr").Style(Styles[Style.hr]).ToHtml())
            }

            return null;
        }

        private string RenderTable(ITabular tabular, TestUnit testUnit, IServiceProvider serviceProvider)
        {
            using (var dataTable = tabular.Create(testUnit))
            {
                var table = Html
                    .Element("table")
                    .Class("table");

                if (tabular.Orientation == TableOrientation.Horizontal)
                {
                    var tr = Html.Element("tr");

                    foreach (var dataColumn in dataTable.Columns.Cast<DataColumn>())
                    {
                        // Normally we would use "th" here but Outlook sucks and renders only crap.
                        tr
                            .Element("th", th => th
                                .Class("cell", "table-header")
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
                        tr.Class("table-footer");
                    }                    

                    foreach (var dataColumn in dataTable.Columns.Cast<DataColumn>())
                    {
                        if (dataColumn.Ordinal == 0 && tabular.Orientation == TableOrientation.Vertical)
                        {
                            // Normally we would use "th" here but Outlook sucks and renders only crap.
                            tr
                                .Element("th", th => th
                                    .Class("cell", "table-header")
                                    .Append(dataRow.Field<string>(dataColumn.ColumnName)));
                        }
                        else
                        {
                            tr
                                .Element("td", td => td
                                    .Class("cell")
                                    .Append(dataRow.Field<string>(dataColumn.ColumnName)));
                        }
                    }

                    tbody.Add(tr);
                }

                table.Add(tbody);

                var cssInliner = serviceProvider.GetService<CssInliner>();
                var css = serviceProvider.GetService<Css>();

                return 
                    table
                        .InlineCss(cssInliner, css)
                        .ToHtml(MarkupFormatting.Empty);
            }
        }
    }
}