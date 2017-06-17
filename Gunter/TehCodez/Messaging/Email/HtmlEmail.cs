using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Gunter.Data;
using Gunter.Messaging.Email.ModuleRenderers;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Logging;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email
{
    [PublicAPI]
    public class HtmlEmail : Alert
    {
        private readonly IEnumerable<ModuleRenderer> _renderers;

        private readonly Func<string, StyleVisitor> _createStyleVisitor;

        private string _to;

        public HtmlEmail(ILogger logger, IEnumerable<ModuleRenderer> renderers, Func<string, StyleVisitor> createStyleVisitor) : base(logger)
        {
            _renderers = renderers;
            _createStyleVisitor = createStyleVisitor;
        }

        [JsonRequired]
        public string To
        {
            get => Variables.Resolve(_to);
            set => _to = value;
        }

        [DefaultValue("Default.css")]
        public string Theme { get; set; }

        [JsonRequired]
        public IEmailClient EmailClient { get; set; }

        protected override void PublishCore(TestUnit testUnit, IReport report)
        {
            var styleVisitor = _createStyleVisitor(Theme);
            var serviceProvider = new ServiceProvider()
                .AddService(styleVisitor)
                .AddService(testUnit.Test.Variables);

            var body = new StringBuilder();

            foreach (var module in report.Modules)
            {
                var renderer = _renderers.Single(r => r.CanRender(module));
                body.AppendLine(renderer.Render(module, testUnit, serviceProvider));
            }

            LogEntry.New().Debug().Message($"To: {To}").Log(Logger);

            var email = new Email<HtmlEmailSubject, HtmlEmailBody>
            {
                Subject = new HtmlEmailSubject(report.Title),
                Body = new HtmlEmailBody
                {
                    Html = body.ToString()
                },
                To = To
            };
            EmailClient.Send(email);
        }
    }

    public class ServiceProvider : IServiceProvider
    {
        private readonly IDictionary<Type, object> _services = new Dictionary<Type, object>();

        public ServiceProvider AddService<TServcie>(TServcie service)
        {
            _services.Add(typeof(TServcie), service);
            return this;
        }

        public object GetService(Type serviceType) => _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}
