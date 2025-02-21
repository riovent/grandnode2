using Grand.Business.Core.Interfaces.Common.Configuration;
using Payments.QRTransfer.Configurations;

namespace Payments.QRTransfer.Services
{
    public class ConfigLoaderService : IConfigLoaderService
    {
        private readonly ISettingService _settingService;

        public ConfigLoaderService(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public async Task<IMAPConfig> CreateIMAPConfig()
        {
            var qrTransferPaymentSettings = await _settingService.LoadSetting<QRTransferPaymentSettings>();

            return new IMAPConfig {
                Host = qrTransferPaymentSettings.ImapHost,
                Port = qrTransferPaymentSettings.ImapPort,
                SecureSocketOptions = qrTransferPaymentSettings.ImapSecureSocketOptions,
                Username = qrTransferPaymentSettings.ImapUsername,
                Password = qrTransferPaymentSettings.ImapPassword
            };
        }
    }
}
