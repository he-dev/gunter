using Gunter.Services.Email.Templates;
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
using Gunter.Data.Sections;

namespace Gunter.Alerts
{
    public class EmailAlert : Alert
    {
        private static readonly Dictionary<Type, ISectionTemplate> _sectionTemplates = new Dictionary<Type, ISectionTemplate>
        {
            [typeof(TextSection)] = new TextTemplate(),
            [typeof(TableSection)] = new TableTemplate(),
        };

        private readonly FooterTemplate _footerRenderer = new FooterTemplate();

        public EmailAlert(ILogger logger) : base(logger) { }

        [JsonRequired]
        public string To { get; set; }

        protected override void PublishCore(IEnumerable<ISection> sections, IConstantResolver constants)
        {
            var body = sections.Select(section => _sectionTemplates[section.GetType()].Render(section, constants)).ToList();
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
