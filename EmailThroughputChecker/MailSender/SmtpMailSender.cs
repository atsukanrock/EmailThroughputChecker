using System;
using System.Net.Mail;

namespace EmailThroughputChecker.MailSender
{
    internal class SmtpMailSender : MailSenderBase
    {
        private readonly SmtpClient _smtpClient;

        public SmtpMailSender() : this(new SmtpClient()) {}

        public SmtpMailSender(SmtpClient smtpClient)
        {
            if (smtpClient == null) throw new ArgumentNullException("smtpClient");

            _smtpClient = smtpClient;
        }

        protected override void DoSend(MailMessage message)
        {
            _smtpClient.Send(message);
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            _smtpClient.Dispose();
        }
    }
}