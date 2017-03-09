using Reusable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gunter.Alerting.Email
{
    internal class EmailAlertBody : EmailBody
    {
        public EmailAlertBody()
        {
            IsHtml = true;
            Encoding = Encoding.UTF8;
            Sections = new List<string>();
        }

        public List<string> Sections { get; set; }        

        public override string ToString() => string.Join(Environment.NewLine, Sections);
    }
}