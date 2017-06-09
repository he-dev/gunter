using System.Text;
using Reusable;

namespace Gunter.Messaging.Email
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