using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Localization;

namespace Payments.QRTransfer;
public class QRTransferWidgetProvider : IWidgetProvider
{
    private readonly QRTransferPaymentSettings _qrTransferPaymentSettings;
    private readonly ITranslationService _translationService;

    public QRTransferWidgetProvider(ITranslationService translationService,
        QRTransferPaymentSettings qrTransferPaymentSettings)
    {
        _translationService = translationService;
        _qrTransferPaymentSettings = qrTransferPaymentSettings;
    }

    public string ConfigurationUrl => QRTransferPaymentDefaults.ConfigurationUrl;

    public string SystemName => QRTransferPaymentDefaults.ProviderSystemName;

    public string FriendlyName => _translationService.GetResource(QRTransferPaymentDefaults.FriendlyName);

    public int Priority => _qrTransferPaymentSettings.DisplayOrder;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    public async Task<IList<string>> GetWidgetZones()
    {
        return await Task.FromResult(new[] { "checkout_payment_info_top" });
    }

    public Task<string> GetPublicViewComponentName(string widgetZone)
    {
        return Task.FromResult("PaymentQRTransferScripts");
    }
}