using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Payments.QRTransfer.Models;

public class ConfigurationModel : BaseModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
{
    public string ActiveStore { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.DescriptionText")]
    public string DescriptionText { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.AdditionalFee")]
    public double AdditionalFee { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.AdditionalFeePercentage")]
    public bool AdditionalFeePercentage { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.ShippableProductRequired")]
    public bool ShippableProductRequired { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.SkipPaymentInfo")]
    public bool SkipPaymentInfo { get; set; }

    public IList<ConfigurationLocalizedModel> Locales { get; set; } = new List<ConfigurationLocalizedModel>();

    #region IMAP Settings

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.ImapUsername")]
    [Required(ErrorMessage = "IMAP Username is required.")]
    public string ImapUsername { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.ImapPassword")]
    [DataType(DataType.Password)]
    public string ImapPassword { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.ImapHost")]
    [Required(ErrorMessage = "IMAP Host is required.")]
    public string ImapHost { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.ImapPort")]
    [Required(ErrorMessage = "IMAP Port is required.")]
    public int ImapPort { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.ImapSecureSocketOptions")]
    public int ImapSecureSocketOptions { get; set; }

    #endregion

    #region Banking and Payment Details

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.FullName")]
    public string FullName { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.IBAN")]
    [Required(ErrorMessage = "IBAN is required.")]
    [StringLength(34, ErrorMessage = "IBAN should be a maximum of 34 characters.")]
    public string IBAN { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.BankCode")]
    public string BankCode { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.IsDynamic")]
    public bool IsDynamic { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.ReferenceNo")]
    public string ReferenceNo { get; set; }

    [GrandResourceDisplayName("Plugins.Payment.QRTransfer.PaymentDescription")]
    public string PaymentDescription { get; set; }

    #endregion

    #region Nested Class for Localization

    public class ConfigurationLocalizedModel : ILocalizedModelLocal
    {
        [GrandResourceDisplayName("Plugins.Payment.QRTransfer.DescriptionText")]
        public string DescriptionText { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.QRTransfer.PaymentDescription")]
        public string PaymentDescription { get; set; }

        public string LanguageId { get; set; }
    }

    #endregion
}
