using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.IOnymous.Http;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Utilities.Mailr;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Services.Messengers
{
    [Gunter]
    [PublicAPI]
    public class Mailr : Messenger
    {
        private readonly IResourceSquid _resources;

        public Mailr
        (
            ILogger<Mailr> logger,
            IResourceSquid resources
        ) : base(logger)
        {
            _resources = resources;
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

            var testResultPath = await _resources.ReadSettingAsync(MailrConfig.TestResultPath);
            await _resources.SendEmailAsync(testResultPath, new UserAgent(ProgramInfo.Name, ProgramInfo.Version), email, "Mailr");
        }
    }
}