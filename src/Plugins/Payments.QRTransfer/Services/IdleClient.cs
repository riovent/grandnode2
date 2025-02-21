using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;

namespace Payments.QRTransfer.Services
{
    public class IdleClient : IDisposable
    {
        private readonly string _host, _username, _password;
        private readonly SecureSocketOptions _sslOptions;
        private readonly int _port;
        private CancellationTokenSource _cancel;
        private CancellationTokenSource _done;
        private readonly ImapClient _client;

        // Public event
        public event EventHandler<EventArgs> CountChanged;
        public event EventHandler<MessageFlagsChangedEventArgs> MessageFlagsChanged;
        public IdleClient(string host, int port, SecureSocketOptions sslOptions, string username, string password, CancellationTokenSource cancel)
        {
            _client = new ImapClient();
            _cancel = cancel;
            _sslOptions = sslOptions;
            _username = username;
            _password = password;
            _host = host;
            _port = port;
        }

        async Task ReconnectAsync()
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(_host, _port, _sslOptions, _cancel.Token);

            if (!_client.IsAuthenticated)
            {
                await _client.AuthenticateAsync(_username, _password, _cancel.Token);

                await _client.Inbox.OpenAsync(FolderAccess.ReadOnly, _cancel.Token);
            }
        }

        async Task WaitForNewMessagesAsync()
        {
            do
            {
                try
                {
                    if (_client.Capabilities.HasFlag(ImapCapabilities.Idle))
                    {
                        // Note: IMAP servers are only supposed to drop the connection after 30 minutes, so normally
                        // we'd IDLE for a max of, say, ~29 minutes... but GMail seems to drop idle connections after
                        // about 10 minutes, so we'll only idle for 9 minutes.
                        _done = new CancellationTokenSource(new TimeSpan(0, 9, 0));
                        try
                        {
                            await _client.IdleAsync(_done.Token, _cancel.Token);
                        }
                        finally
                        {
                            _done?.Dispose();
                            _done = null;
                        }
                    }
                    else
                    {
                        // Note: we don't want to spam the IMAP server with NOOP commands, so lets wait a minute
                        // between each NOOP command.
                        await Task.Delay(new TimeSpan(0, 1, 0), _cancel.Token);
                        await _client.NoOpAsync(_cancel.Token);
                    }
                    break;
                }
                catch (ImapProtocolException)
                {
                    // protocol exceptions often result in the client getting disconnected
                    await ReconnectAsync();
                }
                catch (IOException ex)
                {
                    // I/O exceptions always result in the client getting disconnected
                    await ReconnectAsync();
                }
            } while (true);
        }

        async Task IdleAsync()
        {

            if (!_cancel.IsCancellationRequested)
            {
                try
                {
                    await WaitForNewMessagesAsync();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
            //do
            //{
            //    try
            //    {
            //        await WaitForNewMessagesAsync();
            //    }
            //    catch (OperationCanceledException)
            //    {
            //        break;
            //    }
            //    catch (Exception)
            //    {

            //    }
            //} while (!_cancel.IsCancellationRequested);
        }

        public async Task RunAsync()
        {
            // connect to the IMAP server and get our initial list of messages
            try
            {
                await ReconnectAsync();
                //await FetchMessageSummariesAsync(false);
            }
            catch (OperationCanceledException)
            {
                await _client.DisconnectAsync(true);
                throw;
            }

            // Note: We capture client.Inbox here because cancelling IdleAsync() *may* require
            // disconnecting the IMAP client connection, and, if it does, the `client.Inbox`
            // property will no longer be accessible which means we won't be able to disconnect
            // our event handlers.
            var inbox = _client.Inbox;

            inbox.CountChanged += OnCountChanged;

            //// keep track of flag changes
            inbox.MessageFlagsChanged += OnMessageFlagsChanged;

            await IdleAsync();

            inbox.MessageFlagsChanged -= OnMessageFlagsChanged;

            inbox.CountChanged -= OnCountChanged;

            await _client.DisconnectAsync(true);
        }


        void OnCountChanged(object sender, EventArgs e)
        {
            CountChanged?.Invoke(sender, e);
        }

        void OnMessageFlagsChanged(object sender, MessageFlagsChangedEventArgs e)
        {
            MessageFlagsChanged?.Invoke(sender, e);
        }

        public void Exit()
        {
            _cancel?.Cancel();
        }

        public void Dispose()
        {
            _client?.Dispose();
            _cancel?.Dispose();
        }
    }
}
