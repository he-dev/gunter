using Gunter.Alerts;
using Gunter.Data;
using Gunter.Data.Sections;
using Gunter.Services;
using System;
using System.Collections.Generic;
using System.Data;

namespace Gunter.Tests.Alerting
{
    internal class TestAlert : IAlert
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public List<ISectionFactory> Sections { get; set; } = new List<ISectionFactory>();

        public List<string> Messages { get; set; } = new List<string>();

        public void Publish(IEnumerable<ISection> sections, IConstantResolver constants)
        {
            //Messages.Add(message);
        }

        public void Publish(TestContext testContext, IConstantResolver constants)
        {
            throw new NotImplementedException();
        }
    }
}
