using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reusable;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Gunter.Services.Messengers
{
    [Gunter]
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

        protected override async Task PublishReportAsync(TestContext context, IReport report, IEnumerable<IModuleDto> modules)
        {
            var to = To.Select(x => x.Format(context.RuntimeVariables));
            var subject = report.Title.Format(context.RuntimeVariables);
            var body = new
            {
                Modules = modules
            };

            var email = Email.CreateHtml(to, subject, body, e =>
            {
                e.Theme = Theme;
                e.CC = CC;
            });

            Logger.Log(Abstraction.Layer.Service().Meta(new
            {
                Email = new
                {
                    email.To,
                    email.CC,
                    email.Subject,
                    email.Theme,
                    Modules = body.Modules.Select(m => m.Name)
                }
            }));

            await _resourceProvider.SendAsync(TestResultPath, email, ProgramInfo.Name, ProgramInfo.Version, new JsonSerializer
            {
                Converters =
                {
                    new JsonStringConverter(typeof(SoftString)),
                    //new SoftStringConverter(),
                    new StringEnumConverter()
                }
            });
        }
    }
}