using System.Text;
using Reusable.Net.Mail;

namespace Gunter.Alerting.Emails
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