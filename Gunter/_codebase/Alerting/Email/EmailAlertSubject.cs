using Reusable;
using System.Text;

namespace Gunter.Alerting.Email
{
    internal class EmailAlertSubject : EmailSubject
    {
        public EmailAlertSubject(string source)
        {
            Source = source;
            Encoding = Encoding.UTF8;
        }

        public string Source { get; private set; }

        public override string ToString()
        {
            var result = "Error Notification » {Source}".Format(new { Source });
            return result;
        }
    }
}