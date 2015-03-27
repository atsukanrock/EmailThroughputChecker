using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using EmailThroughputChecker.MailSender;

namespace EmailThroughputChecker
{
    internal class Program
    {
        private static readonly string EmailFrom = ConfigurationManager.AppSettings["EmailFrom"];
        private static readonly string EmailTo = ConfigurationManager.AppSettings["EmailTo"];

        private static int Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                return (int)ExitCode.InvalidCommandLineArgument;
            }

            var messages = GenerateMessages(options.MailCount);

            ThreadPool.SetMinThreads(Math.Max(options.Concurrency, 200), Math.Max(options.Concurrency, 200));
            var successCount = 0;
            var errorCount = 0;

            var stopwatch = Stopwatch.StartNew();
            Task.WhenAll(Enumerable.Range(0, options.Concurrency).Select(i => Task.Run(() =>
            {
                using (var sender = CreateMailSender(options.SendingStrategy))
                {
                    while (true)
                    {
                        MailMessage message;
                        lock (messages)
                        {
                            if (messages.Count == 0) return;
                            message = messages.Dequeue();
                        }

                        try
                        {
                            var sw = Stopwatch.StartNew();

                            sender.Send(message);

                            sw.Stop();
                            Interlocked.Increment(ref successCount);
                            Console.WriteLine(
                                "# of sent emails: {0}, elapsed time: {1} ms, total elapsed time: {2} sec, thread id: {3}",
                                successCount, sw.ElapsedMilliseconds, stopwatch.Elapsed.TotalSeconds,
                                Thread.CurrentThread.ManagedThreadId);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref errorCount);
                            Console.Error.WriteLine(ex);
                        }
                    }
                }
            }))).Wait();

            stopwatch.Stop();
            Console.WriteLine(
                "# of emails: {0}, concurrency: {1}, elapsed time: {2} sec, # of successes: {3}, # of errors: {4}",
                options.MailCount, options.Concurrency, stopwatch.Elapsed.TotalSeconds, successCount, errorCount);

            return (int)ExitCode.Success;
        }

        private static Queue<MailMessage> GenerateMessages(int mailCount)
        {
            return new Queue<MailMessage>(
                Enumerable.Range(0, mailCount)
                          .Select(i => new MailMessage(EmailFrom, EmailTo, "Email throughput test", "test body" + i)));
        }

        private static IMailSender CreateMailSender(SendingStrategy sendingStrategy)
        {
            switch (sendingStrategy)
            {
                case SendingStrategy.AmazonSesApiRaw:
                    return new AmazonSesApiRawMailSender();
                case SendingStrategy.AmazonSesApi:
                    return new AmazonSesApiMailSender();
                case SendingStrategy.AmazonSesApiRawNoSdk:
                    return new AmazonSesApiRawMailSenderNoSdk();
                case SendingStrategy.Smtp:
                    return new SmtpMailSender();
                default:
                    throw new InvalidEnumArgumentException("sendingStrategy", (int)sendingStrategy,
                                                           typeof(SendingStrategy));
            }
        }
    }
}