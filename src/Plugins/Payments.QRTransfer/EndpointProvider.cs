using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Payments.QRTransfer;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(QRTransferPaymentDefaults.PaymentInfo,
            "Plugins/PaymentQRTransfer/PaymentInfo",
            new { controller = "PaymentQRTransfer", action = "PaymentInfo", area = "" }
        );

        endpointRouteBuilder.MapControllerRoute(QRTransferPaymentDefaults.PaymentRedirect,
            "Plugins/PaymentQRTransfer/PaymentRedirect",
            new { controller = "PaymentQRTransfer", action = "PaymentRedirect", area = "" }
        );

        endpointRouteBuilder.MapControllerRoute(QRTransferPaymentDefaults.PaymentProcess,
            "Plugins/PaymentQRTransfer/PaymentProcess",
            new { controller = "PaymentQRTransfer", action = "PaymentProcess", area = "" }
        );

        endpointRouteBuilder.MapControllerRoute(QRTransferPaymentDefaults.PaymentCallback,
            "Plugins/PaymentQRTransfer/PaymentCallback",
            new { controller = "PaymentQRTransfer", action = "PaymentCallback", area = "" }
        );

        endpointRouteBuilder.MapControllerRoute(QRTransferPaymentDefaults.PaymentCompleted,
            "Plugins/PaymentQRTransfer/PaymentCompleted",
            new { controller = "PaymentQRTransfer", action = "PaymentCompleted", area = "" }
        );
    }

    public int Priority => 0;
}