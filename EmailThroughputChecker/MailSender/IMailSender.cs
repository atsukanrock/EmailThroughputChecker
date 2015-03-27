using System;
using System.Net.Mail;

namespace EmailThroughputChecker.MailSender
{
    internal interface IMailSender : IDisposable
    {
        void Send(MailMessage message);
    }
}