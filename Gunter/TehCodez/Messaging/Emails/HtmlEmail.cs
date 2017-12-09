using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Extensions;
using Reusable.IO;
using Reusable.MarkupBuilder;
using Reusable.MarkupBuilder.Html;
using Reusable.Net.Mail;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;

namespace Gunter.Messaging.Emails
{
    [PublicAPI]
    public class HtmlEmail : Message
    {
        private readonly IConfiguration _configuration;
        private readonly IFileSystem _fileSystem;
        private readonly ICssParser _cssParser;
        private readonly IEnumerable<IRenderer> _renderers;
        private readonly ICssInliner _cssInliner;

        private readonly Lazy<Css> _css;

        public HtmlEmail(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IFileSystem fileSystem,
            ICssParser cssParser,
            ICssInliner cssInliner,
            IEnumerable<IRenderer> renderers)
            : base(loggerFactory)
        {
            _configuration = configuration;
            _fileSystem = fileSystem;
            _cssParser = cssParser;
            _renderers = renderers;
            _cssInliner = cssInliner;

            _css = new Lazy<Css>(() =>
            {
                var themesPath = _configuration.Select(() => Program.ThemesDirectoryName);
                themesPath = Path.Combine(themesPath, Theme);
                themesPath = _fileSystem.FindFile(themesPath, _configuration.Select<List<string>>("LookupPaths"));
                var cssText = _fileSystem.ReadAllText(themesPath);
                var css = _cssParser.Parse(cssText);
                return css;
            });
        }

        [JsonRequired]
        public string To { get; set; }

        [DefaultValue("Default.css")]
        public string Theme { get; set; }

        [JsonRequired]
        public IEmailClient EmailClient { get; set; }

        protected override async Task PublishReportAsync(IReport report, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var body = HtmlElement.Builder.Element("body");

            Logger.Log(Category.Snapshot.Arguments(new
            {
                Type = report.GetType().ToPrettyString().ToShortName(),
                ModuleCount = report.Modules.Count,
                ModuleTypes = report.Modules.Select(m => m.GetType().Name)
            }), Layer.Business);

            foreach (var module in report.Modules)
            {
                var renderer = FindRenderer(module);
                foreach (var element in renderer.Render(module, context))
                {
                    body.Add(element);
                }
            }

            Logger.Log(Category.Snapshot.Properties(new {To}), Layer.Business);

            body = _cssInliner.Inline(_css.Value, body);

            var email = new Email<HtmlEmailSubject, HtmlEmailBody>
            {
                Subject = new HtmlEmailSubject(format(report.Title)),
                Body = new HtmlEmailBody
                {
                    Html = body.ToHtml(HtmlFormatting.Empty)
                },
                To = To
            };

            await EmailClient.SendAsync(email);

            IRenderer FindRenderer(IModule module) => _renderers.Single(r => r.CanRender(module));
        }
    }
}
