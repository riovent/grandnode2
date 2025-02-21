namespace Payments.QRTransfer;

public static class QRTransferPaymentDefaults
{
    public const string ProviderSystemName = "Payments.QRTransfer";
    public const string FriendlyName = "Payments.QRTransfer.FriendlyName";
    public const string ConfigurationUrl = "/Admin/PaymentQRTransfer/Configure";
    public static string PaymentInfo => "Plugin.Payments.QRTransfer.PaymentInfo";
    public static string PaymentRedirect => "Plugin.Payments.QRTransfer.PaymentRedirect";
    public static string PaymentProcess => "Plugin.Payments.QRTransfer.PaymentProcess";
    public static string PaymentCallback => "Plugin.Payments.QRTransfer.PaymentCallback";
    public static string PaymentCompleted => "Plugin.Payments.QRTransfer.PaymentCompleted";
    public static string ScheduleTaskName => "Plugin.Payments.QRTransfer.IdleMailScheduleTask";
}