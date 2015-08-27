using System.IO;
using System.Net.Mail;
using System.Reflection;

namespace EmailThroughputChecker
{
    internal static class MailMessageSerializer
    {
        private static readonly ConstructorInfo MailWriterConstructor;
        private static readonly MethodInfo MailMessageSendMethod;
        private static readonly MethodInfo MailWriterCloseMethod;

        static MailMessageSerializer()
        {
            // See https://neildeadman.wordpress.com/2011/02/01/amazon-simple-email-service-example-in-c-sendrawemail/
            // Using reflection for calling internal methods.

            var assembly = typeof(SmtpClient).Assembly;
            var mailWriterType = assembly.GetType("System.Net.Mail.MailWriter");
            MailWriterConstructor = mailWriterType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Stream) }, null);
            MailMessageSendMethod = typeof(MailMessage).GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic);
            MailWriterCloseMethod = mailWriterType.GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static void Serialize(MailMessage mailMessage, Stream stream)
        {
            var mailWriter = MailWriterConstructor.Invoke(new object[] { stream });
            MailMessageSendMethod.Invoke(
                mailMessage, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { mailWriter, true, true },
                null);
            MailWriterCloseMethod.Invoke(
                mailWriter, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { }, null);
        }
    }
}