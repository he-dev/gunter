using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Gunter.Data;
using Gunter.Messaging.Email.Templates;
using Gunter.Reporting;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable;
using Reusable.Logging;

namespace Gunter.Messaging.Email
{
    public class HtmlEmail : Alert
    {
        private readonly TextTemplate _textTemplate = new TextTemplate();
        private readonly TableTemplate _tableTemplate = new TableTemplate();

        private readonly FooterTemplate _footerRenderer = new FooterTemplate();

        [JsonRequired]
        public string To { get; set; }

        [JsonRequired]
        public IEmailClient EmailClient { get; set; }

        public override void Publish(TestContext context)
        {
            var alert = context.Alerts.Single(x => x.Id == Id);

            var reports =
                from id in alert.Reports
                join report in context.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                var sections =
                    from section in report.Sections
                    select new StringBuilder()
                        .Append(_textTemplate.Render(section, context))
                        .Append(_tableTemplate.Render(section, context))
                        .ToString();

                var email = new Email<HtmlEmailSubject, HtmlEmailBody>
                {
                    Subject = new HtmlEmailSubject(context.Constants.Resolve(report.Title)),
                    Body = new HtmlEmailBody
                    {
                        Sections = sections.ToList()
                    },
                    To = context.Constants.Resolve(To)
                };
                EmailClient.Send(email);
            }

            //body.Add(_footerRenderer.Render("Gunter", DateTime.UtcNow));

        }
    }

}
