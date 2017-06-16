using System;
using System.Linq;
using System.Text;
using Gunter.Data;
using Gunter.Messaging.Email.Templates;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Logging;

namespace Gunter.Messaging.Email
{
    [PublicAPI]
    public class HtmlEmail : Alert
    {
        private readonly TextTemplate _textTemplate = new TextTemplate();

        private readonly TableTemplate _tableTemplate = new TableTemplate();

        private readonly FooterTemplate _footerRenderer = new FooterTemplate();

        private string _to;

        public HtmlEmail(ILogger logger) : base(logger) { }        

        [JsonRequired]
        public string To
        {
            get => Variables.Resolve(_to);
            set => _to = value;
        }

        [JsonRequired]
        public IEmailClient EmailClient { get; set; }

        protected override void PublishCore(TestUnit context, IReport report)
        {
            var renderedSections =
                (from section in report.Sections
                 select new StringBuilder()
                     .Append(_textTemplate.Render(context, section))
                     .Append(_tableTemplate.Render(context, section))
                     .ToString()).ToList();

            renderedSections.Add(_footerRenderer.Render(Program.InstanceName, DateTime.UtcNow));

            LogEntry.New().Debug().Message($"To: {To}").Log(Logger);

            var email = new Email<HtmlEmailSubject, HtmlEmailBody>
            {
                Subject = new HtmlEmailSubject(report.Title),
                Body = new HtmlEmailBody
                {
                    Sections = renderedSections
                },
                To = To
            };
            EmailClient.Send(email);
        }
    }
}
