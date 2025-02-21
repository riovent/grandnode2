using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using MediatR;
using Payments.QRTransfer.Constants;
using Payments.QRTransfer.Helpers;
using Payments.QRTransfer.Models;

namespace Payments.QRTransfer.Services
{
    public class QRTransferPaymentService : IQRTransferPaymentService
    {
        private readonly IMediator _mediator;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IOrderService _orderService;
        public QRTransferPaymentService(IMediator mediator, IPaymentTransactionService paymentTransactionService, IOrderService orderService)
        {
            _mediator = mediator;
            _paymentTransactionService = paymentTransactionService;
            _orderService = orderService;
        }

        public async Task<bool> CompletePayment(PaymentCompletedModel completePayment)
        {
            var orderList = await _orderService.SearchOrders(createdFromUtc: DateTime.UtcNow.AddDays(-30), ps: PaymentStatus.Pending);
            var order = orderList.LastOrDefault(p => p.Code.ContainsTarget(completePayment.Description));

            if (order == null)
                return false;

            if (order.OrderStatusId != (int)OrderStatusSystem.Pending)
                return false;

            if (order.PaymentStatusId != PaymentStatus.Pending)
                return false;

            var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);
            if (paymentTransaction == null)
                return false;

            if (paymentTransaction.TransactionStatus != TransactionStatus.Pending)
                return false;

            if (order.UserFields == null)
                return false;

            var senderName = order.UserFields.FirstOrDefault(p => p.Key == OrderConstants.SenderName);
            if (senderName == null)
                return false;

            if (string.IsNullOrWhiteSpace(senderName.Value))
                return false;

            if (!senderName.Value.ContainsTarget(completePayment.SenderName))
                return false;


            if (!order.UserFields.Any(p => p.Key == OrderConstants.SenderIBAN))
                order.UserFields.Add(new() {
                    Key = OrderConstants.SenderIBAN,
                    StoreId = order.StoreId,
                    Value = completePayment.SenderIBAN
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.SenderBankName))
                order.UserFields.Add(new() {
                    Key = OrderConstants.SenderBankName,
                    StoreId = order.StoreId,
                    Value = completePayment.SenderBankName
                });

            if (!order.UserFields.Any(p => p.Key == OrderConstants.RecipientName))
                order.UserFields.Add(new() {
                    Key = OrderConstants.RecipientName,
                    StoreId = order.StoreId,
                    Value = completePayment.RecipientName
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.RecipientBankName))
                order.UserFields.Add(new() {
                    Key = OrderConstants.RecipientBankName,
                    StoreId = order.StoreId,
                    Value = completePayment.RecipientBankName
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.RecipientBranch))
                order.UserFields.Add(new() {
                    Key = OrderConstants.RecipientBranch,
                    StoreId = order.StoreId,
                    Value = completePayment.RecipientBranch
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.RecipientIBAN))
                order.UserFields.Add(new() {
                    Key = OrderConstants.RecipientIBAN,
                    StoreId = order.StoreId,
                    Value = completePayment.RecipientIBAN
                });

            if (!order.UserFields.Any(p => p.Key == OrderConstants.CurrencyCode))
                order.UserFields.Add(new() {
                    Key = OrderConstants.CurrencyCode,
                    StoreId = order.StoreId,
                    Value = completePayment.CurrencyCode
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.Description))
                order.UserFields.Add(new() {
                    Key = OrderConstants.Description,
                    StoreId = order.StoreId,
                    Value = completePayment.Description
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.AmountWords))
                order.UserFields.Add(new() {
                    Key = OrderConstants.AmountWords,
                    StoreId = order.StoreId,
                    Value = completePayment.AmountWords
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.Amount))
                order.UserFields.Add(new() {
                    Key = OrderConstants.Amount,
                    StoreId = order.StoreId,
                    Value = completePayment.Amount.ToString("F2")
                });

            if (!order.UserFields.Any(p => p.Key == OrderConstants.TransactionDate))
                order.UserFields.Add(new() {
                    Key = OrderConstants.TransactionDate,
                    StoreId = order.StoreId,
                    Value = completePayment.TransactionDate.ToString("dd.MM.yyyy HH:mm")
                });
            if (!order.UserFields.Any(p => p.Key == OrderConstants.TransactionCode))
                order.UserFields.Add(new() {
                    Key = OrderConstants.TransactionCode,
                    StoreId = order.StoreId,
                    Value = completePayment.TransactionCode
                });

            if (completePayment.Amount.ToString("F2") != paymentTransaction.TransactionAmount.ToString("F2"))
            {
                var cancelStatus = await _mediator.Send(new CancelOrderCommand { NotifyCustomer = true, NotifyStoreOwner = true, Order = order });
                if (cancelStatus)
                    return true;
            }

            paymentTransaction.AuthorizationTransactionId = order.Code;
            paymentTransaction.AuthorizationTransactionCode = completePayment.TransactionCode;
            paymentTransaction.AuthorizationTransactionResult = "success";
            await _orderService.UpdateOrder(order);
            var result = await _mediator.Send(new MarkAsPaidCommand { PaymentTransaction = paymentTransaction });
            if (result)
                return true;


            return false;

        }

    }
}
