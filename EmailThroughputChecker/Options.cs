using CommandLine;
using CommandLine.Text;

namespace EmailThroughputChecker
{
    internal class Options
    {
        [Option('c', "concurrency", DefaultValue = 16, HelpText = "thread concurrency of sending emails")]
        public int Concurrency { get; set; }

        [Option('m', "mail-count", DefaultValue = 256, HelpText = "number of emails to be sent")]
        public int MailCount { get; set; }

        [Option('s', "strategy", DefaultValue = (int)SendingStrategy.AmazonSesApiRaw,
            HelpText =
                "strategy to send emails: 1) call SendRawEmail API using AWS SDK, 2) call SendEmail API using AWS SDK, 3) call SendRawEmail API 'WITHOUT' using AWS SDK, 4) use SMTP protocol, 5) call DeliverAsync API using SendGrid SDK"
            )]
        public SendingStrategy SendingStrategy { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText { AddDashesToOption = true };
            help.AddPreOptionsLine("Usage: EmailThroughputChecker -c <concurrency> -m <# of emails> -s <strategy>\n");
            help.AddPreOptionsLine("Options:");
            help.AddOptions(this);
            return help;
        }
    }
}