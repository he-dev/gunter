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
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email
{
    [PublicAPI]
    public class HtmlEmail : Alert
    {
        private readonly IHtmlEmailTemplateService _templateService;

        private readonly Func<string, IMarkupVisitor> _createStyleVisitor;

        private string _to;

        public HtmlEmail(ILogger logger, IHtmlEmailTemplateService templateService, Func<string, IMarkupVisitor> createStyleVisitor) : base(logger)
        {
            _templateService = templateService;
            _createStyleVisitor = createStyleVisitor;
        }

        [JsonRequired]
        public string To
        {
            get => Variables.Resolve(_to);
            set => _to = value;
        }

        public string Css { get; set; }

        [JsonRequired]
        public IEmailClient EmailClient { get; set; }

        protected override void PublishCore(TestUnit context, IReport report)
        {
            var styleVisitor = _createStyleVisitor(Css);

            var renderedSections =
                (from section in report.Sections
                 select new StringBuilder()
                     .Append(_templateService.Text.Render(context, section, styleVisitor))
                     .Append(_templateService.Table.Render(context, section, styleVisitor))
                     .ToString()).ToList();

            renderedSections.Add(_templateService.Footer.Render(Program.InstanceName, DateTime.UtcNow));

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

    public interface IHtmlEmailTemplateService
    {
        TextTemplate Text { get; }
        TableTemplate Table { get; }
        FooterTemplate Footer { get; }
    }
}
