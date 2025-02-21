namespace Payments.QRTransfer.Models
{
    public class PaymentCallbackModel
    {
        public string SenderName { get; set; }
        public string QRData { get; set; }
        public Guid OrderGuid { get; set; }
        public string OrderCode { get; set; }
        public string Action { get; set; }
    }
}
