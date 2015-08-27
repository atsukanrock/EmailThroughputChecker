using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using SendGrid;

namespace EmailThroughputChecker.MailSender
{
    internal class SendGridApiMailSender : MailSenderBase
    {
        private readonly ITransport _transport;

        public SendGridApiMailSender()
            : this(ConfigurationManager.AppSettings["SendGridUserName"],
                   ConfigurationManager.AppSettings["SendGridPassword"]) {}

        public SendGridApiMailSender(string userName, string password)
        {
            _transport = new Web(new NetworkCredential(userName, password));
        }

        protected override void DoSend(MailMessage message)
        {
            _transport.DeliverAsync(ConvertMessage(message)).GetAwaiter().GetResult();
        }

        private static ISendGrid ConvertMessage(MailMessage message)
        {
            var result = new SendGridMessage
            {
                From = message.From,
                Subject = message.Subject,
                Text = message.Body
            };
            result.AddTo(message.To.Select(to => to.Address));
            return result;
        }
    }
}