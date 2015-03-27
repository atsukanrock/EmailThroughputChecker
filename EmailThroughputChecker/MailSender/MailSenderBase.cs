using System;
using System.Net.Mail;

namespace EmailThroughputChecker.MailSender
{
    internal abstract class MailSenderBase : IMailSender
    {
        private bool _disposed;

        ~MailSenderBase()
        {
            Dispose(false);
        }

        public void Send(MailMessage message)
        {
            ThrowIfDisposed();

            if (message == null) throw new ArgumentNullException("message");

            DoSend(message);
        }

        protected abstract void DoSend(MailMessage message);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                DisposeManagedResources();
            }

            DisposeUnmanagedResources();

            _disposed = true;
        }

        protected virtual void DisposeManagedResources() {}

        protected virtual void DisposeUnmanagedResources() {}

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
        }
    }
}