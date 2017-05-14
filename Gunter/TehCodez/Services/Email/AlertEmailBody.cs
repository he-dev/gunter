using Reusable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gunter.Services.Email
{
    internal class AlertEmailBody : EmailBody
    {
        public AlertEmailBody()
        {
            IsHtml = true;
            Encoding = Encoding.UTF8;
        }

        public List<string> Sections { get; set; } = new List<string>();

        public override string ToString() => string.Join(Environment.NewLine, Sections);
    }
}