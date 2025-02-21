namespace Payments.QRTransfer.Models
{
    public class PaymentCompletedModel
    {
        public string RecipientName { get; set; }
        public string RecipientBankName { get; set; }
        public string RecipientBranch { get; set; }
        public string CurrencyCode { get; set; }
        public string RecipientIBAN { get; set; }

        public string SenderName { get; set; }
        public string SenderBankName { get; set; }
        public string SenderIBAN { get; set; }


        public DateTime TransactionDate { get; set; }
        public string TransactionCode { get; set; }
        public string Description { get; set; }
        public string AmountWords { get; set; }
        public double Amount { get; set; }
    }
}
