using Gunter.Alerting.Email.Renderers;
using Gunter.Data;
using Gunter.Services;
using Reusable;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gunter.Alerting.Email
{
    public class EmailAlert : Alert
    {
        private readonly MessageRenderer _messageRenderer = new MessageRenderer();

        private readonly SectionRenderer _sectionRenderer = new SectionRenderer();

        private readonly FooterRenderer _footerRenderer = new FooterRenderer();

        public EmailAlert(ILogger logger) : base(logger) { }

        public string To { get; set; }

        protected override void PublishCore(string message, IEnumerable<ISection> sections, IConstantResolver constants)
        {
            var body = new List<string>();
            body.Add(_messageRenderer.Render(message));
            body.AddRange(sections.Select(x => _sectionRenderer.Render(x)));
            body.Add(_footerRenderer.Render("Gunter", DateTime.UtcNow));

            var email = new ErrorEmail
            {
                Subject = new EmailAlertSubject(constants.Resolve(Title)),
                Body = new EmailAlertBody
                {
                    Sections = body
                },
                To = Regex.Split(constants.Resolve(To), "[,;]").Where(x => !string.IsNullOrEmpty(x)).ToList()
            };
            email.Send();
        }
    }

    internal class ErrorEmail : Email<EmailAlertSubject, EmailAlertBody>
    {

    }
}
