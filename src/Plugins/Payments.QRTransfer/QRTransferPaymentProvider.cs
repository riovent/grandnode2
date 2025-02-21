using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Payments.QRTransfer.Models;

namespace Payments.QRTransfer;

public class QRTransferPaymentProvider : IPaymentProvider
{
    private readonly QRTransferPaymentSettings _qrTransferPaymentSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITranslationService _translationService;
    private readonly LinkGenerator _linkGenerator;
    public QRTransferPaymentProvider(
        ITranslationService translationService,
        IHttpContextAccessor httpContextAccessor,
        QRTransferPaymentSettings qrTransferPaymentSettings,
        LinkGenerator linkGenerator)
    {
        _translationService = translationService;
        _httpContextAccessor = httpContextAccessor;
        _qrTransferPaymentSettings = qrTransferPaymentSettings;
        _linkGenerator = linkGenerator;
    }

    public string ConfigurationUrl => QRTransferPaymentDefaults.ConfigurationUrl;

    public string SystemName => QRTransferPaymentDefaults.ProviderSystemName;

    public string FriendlyName => _translationService.GetResource(QRTransferPaymentDefaults.FriendlyName);

    public int Priority => _qrTransferPaymentSettings.DisplayOrder;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    /// <summary>
    ///     Init a process a payment transaction
    /// </summary>
    /// <returns>Payment transaction</returns>
    public async Task<PaymentTransaction> InitPaymentTransaction()
    {
        return await Task.FromResult<PaymentTransaction>(null);
    }

    /// <summary>
    /// Buraya userfileds'a sorgulama numarası eklenecek.
    /// </summary>
    /// <param name="paymentTransaction"></param>
    /// <returns></returns>
    public async Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction)
    {
        var result = new ProcessPaymentResult();
        return await Task.FromResult(result);
    }

    public Task PostProcessPayment(PaymentTransaction paymentTransaction)
    {
        //nothing
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Post redirect payment (used by payment gateways that redirecting to a another URL)
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    public Task<string> PostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        var processUrl = _linkGenerator.GetPathByRouteValues(QRTransferPaymentDefaults.PaymentProcess);
        var callbackUrl = _linkGenerator.GetPathByRouteValues(QRTransferPaymentDefaults.PaymentCallback);
        var paymentRedirectModel = new PaymentRedirectModel {
            ProcessUrl = processUrl,
            CallbackUrl = callbackUrl,
            BankName = "Halkbank",
            PaymentTransactionId = paymentTransaction.Id,
            PaymentMethodSystemName = paymentTransaction.PaymentMethodSystemName,
            TransactionStatus = (int)paymentTransaction.TransactionStatus,
            StoreId = paymentTransaction.StoreId,
            OrderGuid = paymentTransaction.OrderGuid,
            OrderCode = paymentTransaction.OrderCode,
            CustomerId = paymentTransaction.CustomerId,
            CustomerEmail = paymentTransaction.CustomerEmail,
            CurrencyCode = paymentTransaction.CurrencyCode,
            CurrencyRate = paymentTransaction.CurrencyRate,
            TransactionAmount = paymentTransaction.TransactionAmount,
            PaidAmount = paymentTransaction.PaidAmount,
            RefundedAmount = paymentTransaction.RefundedAmount,
            IPAddress = paymentTransaction.IPAddress,
            AuthorizationTransactionId = paymentTransaction.AuthorizationTransactionId,
            AuthorizationTransactionCode = paymentTransaction.AuthorizationTransactionCode,
            AuthorizationTransactionResult = paymentTransaction.AuthorizationTransactionResult,
            CaptureTransactionId = paymentTransaction.CaptureTransactionId,
            CaptureTransactionResult = paymentTransaction.CaptureTransactionResult,
            Description = paymentTransaction.Description,
            AdditionalInfo = paymentTransaction.AdditionalInfo
        };
        var data = System.Text.Json.JsonSerializer.Serialize(paymentRedirectModel);
        var base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        _httpContextAccessor.HttpContext!.Session.SetString("threeDSBase64Content", base64Data);
        var redirectUrl = _linkGenerator.GetPathByRouteValues(QRTransferPaymentDefaults.PaymentRedirect);
        return Task.FromResult(redirectUrl);
    }

    public async Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart)
    {
        if (_qrTransferPaymentSettings.ShippableProductRequired && !cart.RequiresShipping())
            return true;

        return await Task.FromResult(false);
    }

    public async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
    {
        if (_qrTransferPaymentSettings.AdditionalFee <= 0)
            return _qrTransferPaymentSettings.AdditionalFee;

        double result;
        if (_qrTransferPaymentSettings.AdditionalFeePercentage)
        {
            //percentage
            var orderTotalCalculationService = _httpContextAccessor.HttpContext!.RequestServices
                .GetRequiredService<IOrderCalculationService>();
            var subtotal = await orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
            result = (float)subtotal.subTotalWithDiscount * (float)_qrTransferPaymentSettings.AdditionalFee / 100f;
        }
        else
        {
            //fixed value
            result = _qrTransferPaymentSettings.AdditionalFee;
        }

        if (!(result > 0)) return result;
        var currencyService = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<ICurrencyService>();
        var workContext = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IWorkContextAccessor>().WorkContext;
        result = await currencyService.ConvertFromPrimaryStoreCurrency(result, workContext.WorkingCurrency);

        //return result;
        return result;
    }

    public async Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction)
    {
        var result = new CapturePaymentResult();
        result.AddError("Capture method not supported");
        return await Task.FromResult(result);
    }

    public async Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult();
        result.AddError("Refund method not supported");
        return await Task.FromResult(result);
    }

    public async Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction)
    {
        var result = new VoidPaymentResult();
        result.AddError("Void method not supported");
        return await Task.FromResult(result);
    }

    /// <summary>
    ///     Cancel a payment
    /// </summary>
    /// <returns>Result</returns>
    public async Task CancelPayment(PaymentTransaction paymentTransaction)
    {
        var paymentTransactionService = _httpContextAccessor.HttpContext!.RequestServices
            .GetRequiredService<IPaymentTransactionService>();
        paymentTransaction.TransactionStatus = TransactionStatus.Canceled;
        await paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
    }


    public async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);
        if ((DateTime.UtcNow - paymentTransaction.CreatedOnUtc).TotalMinutes < 1)
            return false;

        return await Task.FromResult(true);
    }

    public async Task<IList<string>> ValidatePaymentForm(IDictionary<string, string> model)
    {
        var warnings = new List<string>();
        return await Task.FromResult(warnings);
    }

    /// <summary>
    /// Buraya session olarak sorgulama id'si eklenecek.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<PaymentTransaction> SavePaymentInfo(IDictionary<string, string> model)
    {
        return await Task.FromResult<PaymentTransaction>(null);
    }

    public async Task<bool> SupportCapture()
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> SupportPartiallyRefund()
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> SupportRefund()
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> SupportVoid()
    {
        return await Task.FromResult(false);
    }

    public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

    public async Task<bool> SkipPaymentInfo()
    {
        return await Task.FromResult(_qrTransferPaymentSettings.SkipPaymentInfo);
    }

    public async Task<string> Description()
    {
        return await Task.FromResult(
            _translationService.GetResource("Plugins.Payment.QRTransfer.PaymentMethodDescription"));
    }

    public Task<string> GetControllerRouteName()
    {
        return Task.FromResult(QRTransferPaymentDefaults.PaymentInfo);
    }

    public string LogoURL => "/Plugins/Payments.QRTransfer/logo.jpg";
}