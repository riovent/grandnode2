using Payments.QRTransfer.Models;

namespace Payments.QRTransfer.Services
{
    public interface IQRTransferPaymentService
    {
        Task<bool> CompletePayment(PaymentCompletedModel completePayment);
    }
}
