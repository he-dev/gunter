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

        public HtmlEmail(ILogger logger) : base(logger) { }

        [JsonRequired]
        public string To { get; set; }

        [JsonRequired]
        public IEmailClient EmailClient { get; set; }

        protected override void PublishCore(TestContext context, IReport report)
        {
            var renderedSections =
                (from section in report.Sections
                 select new StringBuilder()
                     .Append(_textTemplate.Render(section, context))
                     .Append(_tableTemplate.Render(section, context))
                     .ToString()).ToList();

            renderedSections.Add(_footerRenderer.Render(Program.InstanceName, DateTime.UtcNow));

            var to = context.Constants.Resolve(To);

            LogEntry.New().Debug().Message($"To: {to}").Log(Logger);

            var email = new Email<HtmlEmailSubject, HtmlEmailBody>
            {
                Subject = new HtmlEmailSubject(context.Constants.Resolve(report.Title)),
                Body = new HtmlEmailBody
                {
                    Sections = renderedSections
                },
                To = to
            };
            EmailClient.Send(email);
        }

    }
}
