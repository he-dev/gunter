using System.Text;
using Reusable.Net.Mail;

namespace Gunter.Alerting.Emails
{
    internal class HtmlEmailSubject : EmailSubject
    {
        public HtmlEmailSubject(string text)
        {
            Text = text;
            Encoding = Encoding.UTF8;
        }

        public string Text { get; }

        public override string ToString() => Text;
    }
}