using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Payments.QRTransfer.Services;

namespace Payments.QRTransfer.Tasks
{
    public class IdleMailScheduleTask : IScheduleTask
    {
        private readonly ILogger<IdleMailScheduleTask> _logger;
        private readonly IConfigLoaderService _configLoaderService;
        private readonly ISenderMessage _senderMessage;
        private static CancellationTokenSource _cancel;
        public IdleMailScheduleTask(ILogger<IdleMailScheduleTask> logger, IConfigLoaderService configLoaderService, ISenderMessage senderMessage)
        {
            _logger = logger;
            _configLoaderService = configLoaderService;
            _senderMessage = senderMessage;
        }

        public async Task Execute()
        {
            TryAction(() => _cancel?.Cancel());
            TryAction(() => _cancel?.Dispose());
            _cancel = null;
            _cancel = new CancellationTokenSource();

            try
            {
                await _senderMessage.SendMessage();
                var config = await _configLoaderService.CreateIMAPConfig();
                using var idleClient = new IdleClient(
                    config.Host,
                    config.Port,
                    (SecureSocketOptions)config.SecureSocketOptions,
                    config.Username,
                    config.Password,
                    _cancel);

                idleClient.MessageFlagsChanged += MessageFlagsChanged;
                idleClient.CountChanged += OnCountChanged;
                await idleClient?.RunAsync();
                idleClient.MessageFlagsChanged -= MessageFlagsChanged;
                idleClient.CountChanged -= OnCountChanged;
                idleClient?.Exit();
                config = null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Cancel idle mail schedule service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error idle mail schedule service. {ExcMessage}", ex.Message);
            }
            finally
            {

            }
        }

        private void OnCountChanged(object? sender, EventArgs e)
        {
            _senderMessage.SendMessage();
        }

        private void MessageFlagsChanged(object? sender, MessageFlagsChangedEventArgs e)
        {

        }

        private void TryAction(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception) { }
        }
    }
}
