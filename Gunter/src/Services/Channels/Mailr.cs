using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;
using Reusable.Utilities.Mailr;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Services.Channels
{
    [Gunter]
    [PublicAPI]
    public class Mailr : Channel
    {
        private readonly IResource _resource;

        public Mailr
        (
            ILogger<Mailr> logger,
            IResource resource
        ) : base(logger)
        {
            _resource = resource;
        }

        [Mergeable]
        public List<string> To { get; set; }

        [Mergeable]
        public List<string> CC { get; set; }

        [DefaultValue("default")]
        [Mergeable]
        public string Theme { get; set; }

        protected override async Task PublishReportAsync(TestContext context, IReport report, IEnumerable<IModuleDto> modules)
        {
            var to = To.Select(x => x.Format(context.RuntimeProperties));
            var subject = report.Title.Format(context.RuntimeProperties);

            modules = modules.ToList();

            var email = new Email.Html(to, subject)
            {
                Theme = Theme,
                CC = CC,
                Body = new
                {
                    Modules = modules
                },
            };

            Logger.Log(Abstraction.Layer.Service().Meta(new
            {
                email.To,
                email.CC,
                email.Subject,
                email.Theme,
                Modules = modules.Select(m => m.Name)
            }, "EmailInfo"));

            var testResultPath = await _resource.ReadSettingAsync(MailrConfig.TestResultPath);
            await _resource.SendEmailAsync(testResultPath, email, http =>
            {
                http.UserAgent = new ProductInfoHeaderValue(ProgramInfo.Name, ProgramInfo.Version);
                http.ControllerTags.Add("Mailr");
            });
        }
    }
}