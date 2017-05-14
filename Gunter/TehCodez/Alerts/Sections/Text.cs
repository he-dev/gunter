using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gunter.Services;
using Reusable.Logging;
using Gunter.Extensions;
using Gunter.Data;
using Gunter.Data.Sections;

namespace Gunter.Alerts.Sections
{
    public class Text : SectionFactory
    {
        public Text(ILogger logger) : base(logger) { }

        protected override ISection CreateCore(TestContext context)
        {
            return new TextSection
            {
                Heading = Heading.Resolve(context.Constants),
                Text = Text.Resolve(context.Constants)
            };
        }
    }
}
