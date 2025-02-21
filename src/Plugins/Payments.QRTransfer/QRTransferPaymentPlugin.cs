using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Infrastructure.Plugins;

namespace Payments.QRTransfer;

/// <summary>
///     QRTransfer payment processor
/// </summary>
public class QRTransferPaymentPlugin(
    ISettingService settingService,
    IScheduleTaskService scheduleTaskService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    #region Methods

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return QRTransferPaymentDefaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        var scheduleTask = await scheduleTaskService.GetTaskByName(QRTransferPaymentDefaults.ScheduleTaskName);
        if (scheduleTask == null)
        {
            await scheduleTaskService.InsertTask(new() {
                ScheduleTaskName = QRTransferPaymentDefaults.ScheduleTaskName,
                Enabled = true,
                StopOnError = false,
                TimeInterval = 1
            });
        }

        // Default settings
        var settings = new QRTransferPaymentSettings {
            DescriptionText =
                "<p>In cases where an order is placed, an authorized representative will contact you, personally or over telephone, to confirm the order.<br />After the order is confirmed, it will be processed.<br />Orders once confirmed, cannot be cancelled.</p><p>P.S. You can edit this text from admin panel.</p>",
            ImapUsername = "transactions@dorlay.com",
            ImapPassword = "Dorlay@411",
            ImapHost = "imap.yandex.com.tr",
            ImapPort = 993,
            ImapSecureSocketOptions = 2,
            FullName = "MUSTAFA ÇELEBİ",
            IBAN = "TR080001200141900001112628",
            BankCode = "0012",
            IsDynamic = true,
            ReferenceNo = "382053517123",
            PaymentDescription = "Ödemenin kabul olması için aşağıdaki kodu QR açıklamasına ekleyiniz."
        };

        await settingService.SaveSetting(settings);

        // Localization resources
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Payments.QRTransfer.FriendlyName", "QR Transfer (QR CODE)");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.DescriptionText", "Description");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.DescriptionText.Hint",
            "Enter info that will be shown to customers during checkout");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.PaymentMethodDescription", "QR Transfer (QR CODE)");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.AdditionalFee", "Additional fee");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.AdditionalFee.Hint", "The additional fee.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.AdditionalFeePercentage", "Additional fee. Use percentage");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.AdditionalFeePercentage.Hint",
            "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ShippableProductRequired", "Shippable product required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ShippableProductRequired.Hint",
            "An option indicating whether shippable products are required in order to display this payment method during checkout.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.SkipPaymentInfo", "Skip payment info");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.DisplayOrder", "Display order");

        // New Localization for additional properties
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ImapUsername", "IMAP Username");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ImapPassword", "IMAP Password");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ImapHost", "IMAP Host");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ImapPort", "IMAP Port");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ImapSecureSocketOptions", "IMAP Secure Socket Options");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.FullName", "Full Name");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.IBAN", "IBAN");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.BankCode", "Bank Code");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.IsDynamic", "Is Dynamic");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.ReferenceNo", "Reference Number");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.QRTransfer.PaymentDescription", "Payment Description");

        await base.Install();
    }

    public override async Task Uninstall()
    {
        var scheduleTask = await scheduleTaskService.GetTaskByName(QRTransferPaymentDefaults.ScheduleTaskName);
        if (scheduleTask != null)
        {
            await scheduleTaskService.DeleteTask(scheduleTask);
        }

        //settings
        await settingService.DeleteSetting<QRTransferPaymentSettings>();

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.DescriptionText");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.DescriptionText.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.PaymentMethodDescription");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.AdditionalFee");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.AdditionalFee.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.AdditionalFeePercentage");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.AdditionalFeePercentage.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ShippableProductRequired");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ShippableProductRequired.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.SkipPaymentInfo");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ImapUsername");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ImapPassword");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ImapHost");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ImapPort");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ImapSecureSocketOptions");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.FullName");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.IBAN");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.BankCode");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.IsDynamic");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.ReferenceNo");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.QRTransfer.PaymentDescription");

        await base.Uninstall();
    }

    #endregion
}
