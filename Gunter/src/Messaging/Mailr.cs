﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Messaging.Abstractions;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.sdk.Http;
using Reusable.sdk.Mailr;
using Reusable.SmartConfig;

namespace Gunter.Messaging
{
    [PublicAPI]
    public class Mailr : Message
    {
        private readonly IResourceProvider _resourceProvider;
        private readonly IRestClient<IMailrClient> _mailrClient;

        public Mailr
        (
            ILogger<Mailr> logger,
            IResourceProvider resourceProvider,
            IRestClient<IMailrClient> mailrClient
        ) : base(logger)
        {
            _resourceProvider = resourceProvider;
            _mailrClient = mailrClient;
        }

        [Mergable]
        public string To { get; set; }

        [DefaultValue("default")]
        [Mergable]
        public string Theme { get; set; }

        protected override async Task PublishReportAsync(TestContext context, IReport report, IEnumerable<(string Name, SectionDto Section)> sections)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var to = format(To);
            var subject = format(report.Title);
            var body = new
            {
                Modules = sections.ToDictionary(t => t.Name, t => t.Section.Dump())
            };

            var email = Email.CreateHtml(to, subject, body, e => e.Theme = Theme);

            Logger.Log(Abstraction.Layer.Infrastructure().Variable(new { email = new { email.To, email.Subject, email.Theme } }));

            await _mailrClient.Resource("Gunter", "Alerts", "TestResult").SendAsync(email);
        }
    }
}