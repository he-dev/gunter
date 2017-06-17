using System;
using System.Collections.Generic;
using System.Text;
using Reusable;

namespace Gunter.Messaging.Email
{
    internal class HtmlEmailBody : EmailBody
    {
        public HtmlEmailBody()
        {
            IsHtml = true;
            Encoding = Encoding.UTF8;
        }

        public string Html { get; set; }

        public override string ToString() => Html;
    }
}