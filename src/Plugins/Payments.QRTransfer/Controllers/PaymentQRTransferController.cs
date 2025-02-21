using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payments.QRTransfer.Constants;
using Payments.QRTransfer.Helpers;
using Payments.QRTransfer.Models;
using Payments.QRTransfer.Services;

namespace Payments.QRTransfer.Controllers;

public class PaymentQRTransferController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly IOrderService _orderService;
    private readonly IQRTransferPaymentService _qRTransferPaymentService;
    private readonly IMediator _mediator;
    public PaymentQRTransferController(
        IWorkContextAccessor workContextAccessor,
        ISettingService settingService,
        IOrderService orderService,
        IPaymentTransactionService paymentTransactionService,
        IMediator mediator,
        IQRTransferPaymentService qRTransferPaymentService)
    {
        _workContextAccessor = workContextAccessor;
        _settingService = settingService;
        _orderService = orderService;
        _mediator = mediator;
        _qRTransferPaymentService = qRTransferPaymentService;
    }

    public async Task<IActionResult> PaymentInfo()
    {
        var qrTransferPaymentSettings = await _settingService.LoadSetting<QRTransferPaymentSettings>(_workContextAccessor.WorkContext.CurrentStore.Id);

        var model = new PaymentInfoModel {
            DescriptionText = qrTransferPaymentSettings.DescriptionText,
        };

        return View("PaymentInfo", model);
    }

    public async Task<IActionResult> PaymentRedirect()
    {
        var base64Data = HttpContext!.Session.GetString("threeDSBase64Content");

        if (string.IsNullOrWhiteSpace(base64Data))
            return RedirectToAction("Index", "Home", new { area = "" });

        var decodedBytes = Convert.FromBase64String(base64Data);

        var utf8String = Encoding.UTF8.GetString(decodedBytes);

        var model = System.Text.Json.JsonSerializer.Deserialize<PaymentRedirectModel>(utf8String);

        var order = await _orderService.GetOrderByGuid(model.OrderGuid);
        if (order == null)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.OrderStatusId != (int)OrderStatusSystem.Pending)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.PaymentStatusId != PaymentStatus.Pending)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.CustomerId != _workContextAccessor.WorkContext.CurrentCustomer.Id)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.StoreId != _workContextAccessor.WorkContext.CurrentStore.Id)
            return RedirectToAction("Index", "Home", new { area = "" });

        return View("PaymentRedirect", model);
    }

    [HttpPost]
    public async Task<IActionResult> PaymentProcess(PaymentRedirectModel paymentRedirect)
    {
        var qrTransferPaymentSettings = await _settingService.LoadSetting<QRTransferPaymentSettings>(_workContextAccessor.WorkContext.CurrentStore.Id);
        if (qrTransferPaymentSettings == null)
            return RedirectToAction("Index", "Home", new { area = "" });

        var order = await _orderService.GetOrderByGuid(paymentRedirect.OrderGuid);
        if (order == null)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.OrderStatusId != (int)OrderStatusSystem.Pending)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.PaymentStatusId != PaymentStatus.Pending)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.CustomerId != _workContextAccessor.WorkContext.CurrentCustomer.Id)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.StoreId != _workContextAccessor.WorkContext.CurrentStore.Id)
            return RedirectToAction("Index", "Home", new { area = "" });

        var paymentProcess = new PaymentProcessModel();

        var qrData = FastQRCodeGenerator.GenerateFastQRCode(
            recipientName: qrTransferPaymentSettings.FullName,
            recipientIBAN: qrTransferPaymentSettings.IBAN,
            bankCode: qrTransferPaymentSettings.BankCode,
            amount: Math.Round(Convert.ToDecimal(paymentRedirect.TransactionAmount), 2),
            createdDate: DateTime.UtcNow,
            expiryDate: DateTime.UtcNow.AddDays(30),
            isDynamic: qrTransferPaymentSettings.IsDynamic,
            referenceNo: qrTransferPaymentSettings.ReferenceNo
        );

        //string qrData = FastQRCodeGenerator.GenerateFastQRCode(
        //    recipientName: "MUSTAFA ÇELEBİ",
        //    recipientIBAN: "TR080001200141900001112628",
        //    bankCode: "0012",
        //    amount: Math.Round(Convert.ToDecimal(paymentRedirect.TransactionAmount), 2),
        //    createdDate: DateTime.Now,
        //    expiryDate: DateTime.Parse("2030-02-15 16:16:57"),
        //    isDynamic: true,
        //    referenceNo: "382053517123"
        //);

        //var qrData = FastQRCodeGenerator.GenerateFastQRCode(
        //    recipientName: "MUSTAFA ÇELEBİ",
        //    recipientIBAN: "TR080001200141900001112628",
        //    bankCode: "0012",
        //    amount: Math.Round(Convert.ToDecimal(paymentRedirect.TransactionAmount), 2),
        //    createdDate: DateTime.Now,
        //    expiryDate: DateTime.Parse("2030-02-15 16:16:57"),
        //    isDynamic: true,
        //    referenceNo: "382053517123"
        //);

        paymentProcess.Description = qrTransferPaymentSettings.PaymentDescription;
        paymentProcess.OrderCode = paymentRedirect.OrderCode;
        paymentProcess.OrderGuid = paymentRedirect.OrderGuid;
        paymentProcess.TransactionAmount = paymentRedirect.TransactionAmount;
        paymentProcess.FirstName = order.BillingAddress.FirstName;
        paymentProcess.LastName = order.BillingAddress.LastName;
        paymentProcess.QRData = qrData;
        paymentProcess.CallbackUrl = paymentRedirect.CallbackUrl;
        return View("PaymentProcess", paymentProcess);
    }

    [HttpPost]
    public async Task<IActionResult> PaymentCallback(PaymentCallbackModel paymentCallback)
    {
        var order = await _orderService.GetOrderByGuid(paymentCallback.OrderGuid);
        if (order == null)
            return RedirectToAction("Index", "Home", new { area = "" });

        if(order.OrderStatusId != (int)OrderStatusSystem.Pending)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.PaymentStatusId != PaymentStatus.Pending)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (order.CustomerId != _workContextAccessor.WorkContext.CurrentCustomer.Id)
            return RedirectToAction("Index", "Home", new { area = "" });

        if(order.StoreId != _workContextAccessor.WorkContext.CurrentStore.Id)
            return RedirectToAction("Index", "Home", new { area = "" });

        if (paymentCallback.Action == "confirm")
        {
            var qrData = order.UserFields.FirstOrDefault(p => p.Key == OrderConstants.QRData);
            if(qrData == null)
            {
                order.UserFields.Add(new Grand.Domain.Common.UserField {
                    StoreId = order.StoreId,
                    Key = OrderConstants.QRData,
                    Value = paymentCallback.QRData
                });
                order.UserFields.Add(new Grand.Domain.Common.UserField {
                    StoreId = order.StoreId,
                    Key = OrderConstants.SenderName,
                    Value = paymentCallback.SenderName
                });
                await _orderService.UpdateOrder(order);
            }
            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }
        else
        {
            await _mediator.Send(new CancelOrderCommand { NotifyCustomer = false, NotifyStoreOwner = false, Order = order });
        }
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    [HttpPost]
    public async Task<IActionResult> PaymentCompleted(PaymentCompletedModel paymentCompleted)
    {
        var status = await _qRTransferPaymentService.CompletePayment(paymentCompleted);
        if (status)
            return Ok();

        return BadRequest();
    }
}