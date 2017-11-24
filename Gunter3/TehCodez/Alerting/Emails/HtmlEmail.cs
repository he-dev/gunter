using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.MarkupBuilder;
using Reusable.MarkupBuilder.Html;
using Reusable.Net.Mail;
using Reusable.OmniLog;
using Reusable.SmartConfig;

namespace Gunter.Alerting.Emails
{
    [PublicAPI]
    public class HtmlEmail : Alert
    {
        private readonly IConfiguration _configuration;
        private readonly IPathResolver _pathResolver;
        private readonly IFileSystem _fileSystem;
        private readonly ICssParser _cssParser;
        private readonly IEnumerable<ModuleRenderer> _renderers;
        private readonly CssInliner _cssInliner;

        private readonly Lazy<Css> _css;

        private string _to;

        public HtmlEmail(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IPathResolver pathResolver,
            IFileSystem fileSystem,
            ICssParser cssParser,
            CssInliner cssInliner,
            IEnumerable<ModuleRenderer> renderers)
            : base(loggerFactory)
        {
            _configuration = configuration;
            _pathResolver = pathResolver;
            _fileSystem = fileSystem;
            _cssParser = cssParser;
            _renderers = renderers;
            _cssInliner = cssInliner;

            _css = new Lazy<Css>(() =>
            {
                var themesPath = _configuration.Select(() => Program.ThemesDirectoryName);
                themesPath = Path.Combine(themesPath, Theme);
                themesPath = _pathResolver.ResolveFilePath(themesPath);
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

        protected override async Task PublishReport(IReport report, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var body = HtmlElement.Builder.Element("body");

            foreach (var module in report.Modules)
            {
                var renderer = FindRenderer(module);
                var elements = renderer.Render(module, context);
                body.Add(elements);
            }

            //Logger.Log(e => e.Message($"Sending report {report.Id} to \"{To}\"."));

            _cssInliner.Inline(_css.Value, body);

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

            IModuleRenderer FindRenderer(IModule module) => _renderers.Single(r => r.CanRender(module));
        }
    }

    //public class ServiceProvider : IServiceProvider
    //{
    //    private readonly IDictionary<Type, object> _services = new Dictionary<Type, object>();

    //    public ServiceProvider AddService<TServcie>(TServcie service)
    //    {
    //        _services.Add(typeof(TServcie), service);
    //        return this;
    //    }

    //    public object GetService(Type serviceType) => _services.TryGetValue(serviceType, out var service) ? service : null;
    //}
}
