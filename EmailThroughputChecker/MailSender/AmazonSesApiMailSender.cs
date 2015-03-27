using System.Linq;
using System.Net.Mail;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace EmailThroughputChecker.MailSender
{
    internal class AmazonSesApiMailSender : AmazonSesApiMailSenderBase
    {
        public AmazonSesApiMailSender() {}

        public AmazonSesApiMailSender(AmazonSimpleEmailServiceClient client) : base(client) {}

        protected override void DoSend(MailMessage message)
        {
            Client.SendEmail(ConvertMessageToRequest(message));
        }

        private static SendEmailRequest ConvertMessageToRequest(MailMessage mailMessage)
        {
            return new SendEmailRequest(
                mailMessage.From.Address,
                new Destination(mailMessage.To.AsEnumerable().Select(x => x.Address).ToList()),
                new Message(new Content(mailMessage.Subject), new Body(new Content(mailMessage.Body))));
        }
    }
}