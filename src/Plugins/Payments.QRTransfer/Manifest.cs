using Grand.Infrastructure.Plugins;
using Payments.QRTransfer;

[assembly: PluginInfo(
    FriendlyName = "QR Transfer (QR CODE)",
    Group = "Payment methods",
    SystemName = QRTransferPaymentDefaults.ProviderSystemName,
    Author = "dorlay team",
    Version = "2.1.1"
)]