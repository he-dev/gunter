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
using Reusable.IOnymous.Config;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Quickey;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Gunter.Services.Messengers
{
    [Gunter]
    [PublicAPI]
    public class Mailr : Messenger
    {
        private readonly IResourceProvider _resources;

        public Mailr
        (
            ILogger<Mailr> logger,
            IResourceProvider resources
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

            var testResultPath = await _resources.ReadSettingAsync(From<IMailrConfig>.Select(x => x.TestResultPath));
            await _resources.SendAsync(testResultPath, email, ProgramInfo.Name, ProgramInfo.Version, new JsonSerializer
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