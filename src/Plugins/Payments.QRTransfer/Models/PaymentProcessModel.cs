namespace Payments.QRTransfer.Models
{
    public class PaymentProcessModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string QRData { get; set; }


        public string PaymentTransactionId { get; set; }
        public string BankName { get; set; }
        public string ProcessUrl { get; set; }
        public string CallbackUrl { get; set; }
        public string PaymentMethodSystemName { get; set; }
        public int TransactionStatus { get; set; }
        public string StoreId { get; set; }
        public Guid OrderGuid { get; set; }
        public string OrderCode { get; set; }
        public string CustomerId { get; set; }
        public string CustomerEmail { get; set; }
        public string CurrencyCode { get; set; }
        public double CurrencyRate { get; set; }
        public double TransactionAmount { get; set; }
        public double PaidAmount { get; set; }
        public double RefundedAmount { get; set; }
        public string IPAddress { get; set; }
        public string AuthorizationTransactionId { get; set; }
        public string AuthorizationTransactionCode { get; set; }
        public string AuthorizationTransactionResult { get; set; }
        public string CaptureTransactionId { get; set; }
        public string CaptureTransactionResult { get; set; }
        public string Description { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
