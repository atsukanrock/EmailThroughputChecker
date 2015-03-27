using System.IO;
using System.Linq;
using System.Net.Mail;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace EmailThroughputChecker.MailSender
{
    internal class AmazonSesApiRawMailSender : AmazonSesApiMailSenderBase
    {
        public AmazonSesApiRawMailSender() {}

        public AmazonSesApiRawMailSender(AmazonSimpleEmailServiceClient client) : base(client) {}

        protected override void DoSend(MailMessage message)
        {
            Client.SendRawEmail(ConvertMessageToRequest(message));
        }

        private static SendRawEmailRequest ConvertMessageToRequest(MailMessage mailMessage)
        {
            using (var stream = new MemoryStream())
            {
                MailMessageSerializer.Serialize(mailMessage, stream);
                return new SendRawEmailRequest(new RawMessage(stream))
                {
                    Destinations = mailMessage.To.AsEnumerable().Select(x => x.Address).ToList(),
                    Source = mailMessage.From.Address
                };
            }
        }
    }
}