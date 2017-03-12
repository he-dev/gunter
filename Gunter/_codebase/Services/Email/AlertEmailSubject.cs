using Reusable;
using System.Text;

namespace Gunter.Services.Email
{
    internal class AlertEmailSubject : EmailSubject
    {
        public AlertEmailSubject(string text)
        {
            Text = text;
            Encoding = Encoding.UTF8;
        }

        public string Text { get; }

        public override string ToString() => Text;
    }
}