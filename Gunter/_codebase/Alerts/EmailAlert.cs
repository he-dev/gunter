using Gunter.Services.Email.Renderers;
using Gunter.Data;
using Gunter.Services;
using Gunter.Services.Email;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gunter.Alerts
{
    public class EmailAlert : Alert
    {
        private readonly MessageRenderer _messageRenderer = new MessageRenderer();

        private readonly SectionRenderer _sectionRenderer = new SectionRenderer();

        private readonly FooterRenderer _footerRenderer = new FooterRenderer();

        public EmailAlert(ILogger logger) : base(logger) { }

        [JsonRequired]
        public string To { get; set; }

        protected override void PublishCore(string message, IEnumerable<ISection> sections, IConstantResolver constants)
        {
            var body = new List<string>();
            body.Add(_messageRenderer.Render(message));
            body.AddRange(sections.Select(x => _sectionRenderer.Render(x)));
            body.Add(_footerRenderer.Render("Gunter", DateTime.UtcNow));

            var email = new AlertEmail
            {
                Subject = new AlertEmailSubject(constants.Resolve(Title)),
                Body = new AlertEmailBody
                {
                    Sections = body
                },
            };
            var to = constants.Resolve(To);
            email.Send(to);
        }
    }

}
