using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;

namespace Gunter.Services.Messengers
{
    [PublicAPI]
    public class Mailr : Messenger
    {
        private readonly IConfiguration _configuration;
        private readonly IResourceProvider _resourceProvider;

        public Mailr
        (
            ILogger<Mailr> logger,
            IConfiguration configuration,
            IResourceProvider resourceProvider
        ) : base(logger)
        {
            _configuration = configuration;
            _resourceProvider = resourceProvider;
        }

        [Mergeable]
        public IList<string> To { get; set; }
        
        [Mergeable]
        public IList<string> CC { get; set; }

        [DefaultValue("default")]
        [Mergeable]
        public string Theme { get; set; }

        [JsonIgnore]
        [SettingMember(Prefix = "mailr", Strength = SettingNameStrength.Low)]
        public string TestResultPath => _configuration.GetSetting(() => TestResultPath);

        protected override async Task PublishReportAsync(TestContext context, IReport report, IEnumerable<(string Name, ModuleDto Section)> sections)
        {
            var to = To.Select(x => x.Format(context.RuntimeVariables));
            var subject = report.Title.Format(context.RuntimeVariables);
            var body = new
            {
                Modules = sections.ToDictionary(t => t.Name, t => t.Section)
            };

            var email = Email.CreateHtml(to, subject, body, e => e.Theme = Theme);

            Logger.Log(Abstraction.Layer.Infrastructure().Meta(new { Email = new { email.To, email.Subject, email.Theme, Modules = body.Modules.Select(m => m.Key) } }));

            await _resourceProvider.SendAsync(TestResultPath, email, ProgramInfo.Name, ProgramInfo.Version);
        }
    }
}