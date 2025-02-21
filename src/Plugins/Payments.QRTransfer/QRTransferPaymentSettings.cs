using Grand.Domain.Configuration;

namespace Payments.QRTransfer;

public class QRTransferPaymentSettings : ISettings
{
    public int DisplayOrder { get; set; }

    public string DescriptionText { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false -
    ///     fixed value.
    /// </summary>
    public bool AdditionalFeePercentage { get; set; }

    /// <summary>
    ///     Additional fee
    /// </summary>
    public double AdditionalFee { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether shippable products are required in order to display this payment method
    ///     during checkout
    /// </summary>
    public bool ShippableProductRequired { get; set; }

    public bool SkipPaymentInfo { get; set; }

    public string ImapUsername { get; set; }
    public string ImapPassword { get; set; }
    public string ImapHost { get; set; }
    public int ImapPort { get; set; }
    public int ImapSecureSocketOptions { get; set; }

    public string FullName { get; set; }
    public string IBAN { get; set; }
    public string BankCode { get; set; }
    public bool IsDynamic { get; set; }
    public string ReferenceNo { get; set; }

    public string PaymentDescription { get; set; }
}