using System;
using Amazon;
using Amazon.SimpleEmail;

namespace EmailThroughputChecker.MailSender
{
    internal abstract class AmazonSesApiMailSenderBase : MailSenderBase
    {
        private readonly AmazonSimpleEmailServiceClient _client;

        protected AmazonSimpleEmailServiceClient Client
        {
            get { return _client; }
        }

        protected AmazonSesApiMailSenderBase()
            : this(new AmazonSimpleEmailServiceClient(RegionEndpoint.USEast1)) {}

        protected AmazonSesApiMailSenderBase(AmazonSimpleEmailServiceClient client)
        {
            if (client == null) throw new ArgumentNullException("client");
            _client = client;
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            _client.Dispose();
        }
    }
}