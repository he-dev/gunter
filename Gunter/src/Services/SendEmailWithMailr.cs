using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Gunter.Data.Configuration.Tasks;
using Gunter.Services.Abstractions;
using Reusable.Extensions;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Extensions;
using Reusable.Translucent;
using Reusable.Utilities.Mailr;

namespace Gunter.Services
{
    public class SendEmailWithMailr
    {
        public SendEmailWithMailr(ILogger<SendEmailWithMailr> logger, IResource resource, ITryGetFormatValue tryGetFormatValue)
        {
            Resource = resource;
            Logger = logger;
            TryGetFormatValue = tryGetFormatValue;
        }

        private ILogger<SendEmailWithMailr> Logger { get; }

        private IResource Resource { get; }

        private ITryGetFormatValue TryGetFormatValue { get; }

        public async Task ExecuteAsync(SendEmail sendEmail, IEnumerable<IReportSectionDto> modules)
        {
            var to = sendEmail.To.Select(x => x.Format(TryGetFormatValue));
            var subject = sendEmail.Subject.Format(TryGetFormatValue);

            var htmlEmail = new Reusable.Utilities.Mailr.Models.Email.Html(to, subject)
            {
                //Theme = Theme,
                //CC = CC,
                Subject = subject,
                Body = new
                {
                    Modules = modules
                },
            };

            Logger.Log(Telemetry.Collect.Application().Metadata("Email", new { htmlEmail.To, htmlEmail.CC, htmlEmail.Subject }));

            var testResultPath = await Resource.ReadSettingAsync(MailrConfig.TestResultPath);
            await Resource.SendEmailAsync(testResultPath, htmlEmail, http =>
            {
                http.UserAgent = new ProductInfoHeaderValue(ProgramInfo.Name, ProgramInfo.Version);
                http.ControllerName = "Mailr";
            });
        }
    }
}