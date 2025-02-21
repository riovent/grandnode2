using Microsoft.AspNetCore.Mvc;

namespace Payments.QRTransfer.Components;

[ViewComponent(Name = "PaymentQRTransferScripts")]
public class PaymentQRTransferScripts : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
